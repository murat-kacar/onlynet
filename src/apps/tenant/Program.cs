using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Radzen;
using Serilog;
using System.Globalization;
using TabFlow.Shared.Application.EventBus;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;
using TabFlow.Shared.Infrastructure.Diagnostics;
using TabFlow.Tenant.Cli;
using TabFlow.Tenant.Middleware;
using TabFlow.Tenant.Services;
using TabFlow.Tenant.WebSocket;
using TabFlow.Tenant.Hubs;

if (args.Length > 0 && args[0] == "bootstrap-owner")
{
    await BootstrapOwnerCommand.RunAsync(args.Skip(1).ToArray());
    return;
}

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("/var/log/tabflow/tenant-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting TabFlow Tenant");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // TD-0026: register Systemd lifetime so the host signals readiness
    // to systemd via sd_notify("READY=1") only after ASP.NET Core
    // completes startup. The reference unit set in
    // /doc/docs/how-to/supervise-processes.md uses Type=notify and
    // depends on this. UseSystemd() is a no-op when INVOCATION_ID is
    // unset (i.e. outside systemd), so it is safe in dotnet run and
    // in tests.
    builder.Host.UseSystemd();

builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TenantDb")));
builder.Services.AddDbContextFactory<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TenantDb")));

builder.Services.AddSingleton<TableWebSocketHandler>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
builder.Services.AddScoped<ICustomerSessionService, CustomerSessionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITableReadService, TableReadService>();
builder.Services.AddScoped<ITableCommandService, TableCommandService>();
builder.Services.AddScoped<IMenuReadService, MenuReadService>();
builder.Services.AddScoped<IKitchenReadService, KitchenReadService>();
builder.Services.AddHostedService<EventSubscriptionService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<TenantUserPreferenceService>();
builder.Services.AddScoped<TenantAdminActivationService>();
builder.Services.AddScoped<CustomerSessionBrowserStore>();

builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<TenantDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 4;
    options.SignIn.RequireConfirmedEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";

    // Cookie auth defaults to 302-redirect on a missing or
    // unauthorised principal. That is the right answer for HTML
    // page navigations but the wrong answer for API callers, who
    // expect 401/403 status codes. The handlers below short-circuit
    // the redirect for any path under `/api/` and let HTML routes
    // continue to redirect to the configured LoginPath /
    // AccessDeniedPath. Tracked under TD-0015 step 5.
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Tenant:Read", policy => policy.RequireClaim("TenantRole", "Read"));
    options.AddPolicy("Tenant:Write", policy => policy.RequireClaim("TenantRole", "Write"));
    options.AddPolicy("Tenant:Self", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddLocalization();
builder.Services.AddRadzenComponents();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TabFlow.Tenant"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigationManager.BaseUri),
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Health checks per /doc/docs/reference/architecture/health-checks.md.
// /health/live carries no probes (liveness only). /health/ready runs
// the probe set tagged "ready". Additional probes (migration head,
// event-bus capacity, tenant-context) are tracked under TD-0013's
// payoff plan.
string[] readyTag = ["ready"];
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TenantDbContext>(
        name: "tenant-db:ping",
        tags: readyTag)
    // TD-0013 step 2: migration-head probe surfaces an out-of-date
    // database against the running binary's migration set.
    .AddCheck<MigrationHeadHealthCheck<TenantDbContext>>(
        name: "tenant-db:migrations",
        tags: readyTag)
    // TD-0013 step 4: event-bus capacity probe surfaces subscriber
    // saturation on the in-process bus (AD-0006).
    .AddCheck<EventBusCapacityHealthCheck>(
        name: "event-bus:capacity",
        tags: readyTag)
    // TD-0013 step 5: tenant-context probe surfaces a tenant host
    // launched without its provisioning contract
    // (TABFLOW_TENANT_CODE env var).
    .AddCheck<TenantContextHealthCheck>(
        name: "tenant-context",
        tags: readyTag);

// AD-0004: Blazor Web App composition. AddRazorComponents
// registers the new component model (root component +
// MapRazorComponents<App>()); AddInteractiveServerComponents adds
// the SignalR-backed interactive server render mode that staff
// pages opt into via `@rendermode InteractiveServer`. AddRazorPages
// stays so Identity's Login.cshtml and ChangePassword.cshtml keep
// rendering as classic Razor Pages.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

var supportedCultures = new List<string> { "en-GB", "tr-TR" }
    .Select(name => new CultureInfo(name))
    .ToList();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-GB"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
};

localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new TenantUserPreferenceCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider(),
};

if (!app.Environment.IsEnvironment("Testing") && app.Urls.Count == 0)
{
    app.Urls.Add("http://localhost:5001");
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();
app.UseRequestLocalization(localizationOptions);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SecurityEnrollmentRequiredMiddleware>();
app.UseAntiforgery();

app.MapControllers();

app.MapGet("/ws/tables/{tableNumber:int}", async (HttpContext context, int tableNumber, TableWebSocketHandler handler) =>
{
    await handler.HandleAsync(context, tableNumber);
});

// AC-101 requires `/health`, `/health/live`, and `/health/ready`.
// `/health` is registered as an alias for liveness (same handler, no
// probes) so that callers using the bare path also receive a useful
// answer; the architectural spec at
// /doc/docs/reference/architecture/health-checks.md only mandates the
// two namespaced endpoints. Keep the three in sync.
var livenessOptions = new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthJsonWriter.Write,
};
var readinessOptions = new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthJsonWriter.Write,
};

app.MapHealthChecks("/health", livenessOptions).AllowAnonymous();
app.MapHealthChecks("/health/live", livenessOptions).AllowAnonymous();
app.MapHealthChecks("/health/ready", readinessOptions).AllowAnonymous();

app.MapHub<TenantHub>("/hub/tenant");

// Blazor Web App framework assets (`/_framework/blazor.web.js`,
// compressed variants, fingerprinted CSS) are described by the
// static-web-assets manifest generated at build time. MapStaticAssets
// projects that manifest into runtime endpoints; UseStaticFiles alone
// only serves the app's physical wwwroot tree.
app.MapStaticAssets();

app.MapGet("/Account/Login", (HttpRequest request) =>
    Results.Redirect($"/login{request.QueryString}")).AllowAnonymous();
app.MapGet("/Account/AccessDenied", () =>
    Results.Redirect("/login")).AllowAnonymous();

app.MapRazorPages();

// AD-0004: Blazor Web App route mapping. MapRazorComponents
// hosts the App root component (the HTML document) and serves every
// `@page` Razor component under it. AddInteractiveServerRenderMode
// wires the SignalR endpoint that components annotated with
// `@rendermode InteractiveServer` connect to; without that call,
// the annotation has no effect.
app.MapRazorComponents<TabFlow.Tenant.Components.App>()
    .AddInteractiveServerRenderMode();

Log.Information("TabFlow Tenant started successfully");
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TabFlow Tenant terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// `public partial class Program` exposes the auto-generated entry
// point so test projects can target the host when they need an
// in-process ASP.NET Core factory.
public partial class Program { }
