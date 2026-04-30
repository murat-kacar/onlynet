using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Radzen;
using Serilog;
using System.Globalization;
using TabFlow.Platform.Cli;
using TabFlow.Platform.Middleware;
using TabFlow.Platform.Services;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;
using TabFlow.Shared.Infrastructure.Diagnostics;

// CLI subcommands are dispatched before the web host starts so that
// they do not have to load the Serilog file sink, the OpenTelemetry
// pipeline, or the cookie-auth configuration. Each subcommand owns
// its own minimal Generic Host with only the DI registrations it
// needs (see /doc/docs/how-to/bootstrap-platform.md).
if (args.Length > 0 && args[0] == "bootstrap-admin")
{
    return await BootstrapAdminCommand.RunAsync(args.Skip(1).ToArray());
}

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("/var/log/tabflow/platform-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting TabFlow Platform");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    var identityOptions = builder.Configuration.GetSection(PlatformIdentityOptions.SectionName).Get<PlatformIdentityOptions>() ?? new PlatformIdentityOptions();

    // TD-0026: register Systemd lifetime so the host signals readiness
    // to systemd via sd_notify("READY=1") only after ASP.NET Core
    // completes startup. The reference unit set in
    // /doc/docs/how-to/supervise-processes.md uses Type=notify and
    // depends on this. UseSystemd() is a no-op when INVOCATION_ID is
    // unset (i.e. outside systemd), so it is safe in dotnet run and
    // in tests.
    builder.Host.UseSystemd();

builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PlatformDb")));
builder.Services.AddDbContextFactory<PlatformDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PlatformDb")));

builder.Services.AddScoped<IPlatformAuditService, PlatformAuditService>();
builder.Services.AddScoped<IPlatformAuditReadService, PlatformAuditReadService>();
// Read-path services own PlatformDbContext access so API controllers
// stay thin transport adapters.
builder.Services.AddScoped<ITenantRegistryService, TenantRegistryService>();
builder.Services.AddScoped<IProvisioningJobReadService, ProvisioningJobReadService>();
builder.Services.AddSingleton<PlatformUserIdentityService>();
builder.Services.AddScoped<PlatformClaimsTransformation>();
builder.Services.AddScoped<PlatformUserPreferenceService>();
builder.Services.Configure<PlatformIdentityOptions>(builder.Configuration.GetSection(PlatformIdentityOptions.SectionName));

builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<PlatformDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IClaimsTransformation, PlatformClaimsTransformation>();

if (identityOptions.EnableExternalIdentity)
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = identityOptions.Authority;
            options.ClientId = identityOptions.ClientId;
            options.ClientSecret = identityOptions.ClientSecret;
            options.CallbackPath = identityOptions.CallbackPath;
            options.SignedOutCallbackPath = identityOptions.SignedOutCallbackPath;
            options.RequireHttpsMetadata = identityOptions.RequireHttpsMetadata;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;
            options.SignInScheme = IdentityConstants.ApplicationScheme;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.TokenValidationParameters.NameClaimType = "preferred_username";
            options.Events = new OpenIdConnectEvents
            {
                OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    context.Response.Redirect("/login?error=oidc");
                    return Task.CompletedTask;
                }
            };
        });
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
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
    options.AddPolicy("Platform:Read", policy => policy.RequireClaim("PlatformRole", "Read"));
    options.AddPolicy("Platform:Write", policy => policy.RequireClaim("PlatformRole", "Write"));
    options.AddPolicy("Platform:Self", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRadzenComponents();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TabFlow.Platform"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Health checks per /doc/docs/reference/architecture/health-checks.md.
// /health/live carries no probes (liveness only). /health/ready runs
// the probe set tagged "ready". Additional probes (migration head,
// worker heartbeat) are tracked under TD-0013's payoff plan.
string[] readyTag = ["ready"];
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PlatformDbContext>(
        name: "platform-db:ping",
        tags: readyTag)
    // TD-0013 step 2: migration-head probe surfaces an out-of-date
    // database against the running binary's migration set.
    .AddCheck<MigrationHeadHealthCheck<PlatformDbContext>>(
        name: "platform-db:migrations",
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
builder.Services.AddLocalization();
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
    new PlatformUserPreferenceCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider(),
};

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();
app.UseRequestLocalization(localizationOptions);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<AuditMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

// TD-0002 step 3: any authenticated principal carrying the
// must-change-password claim is bounced through /change-password
// until the claim is removed by ChangePasswordModel.OnPostAsync. The
// middleware sits after UseAuthorization so HttpContext.User is fully
// populated, and before the route mapping below so the redirect wins
// over the original handler.
if (!identityOptions.EnableExternalIdentity)
{
    app.UseMiddleware<PasswordChangeRequiredMiddleware>();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/menu") ||
        context.Request.Path.StartsWithSegments("/g") ||
        context.Request.Path.StartsWithSegments("/api/public"))
    {
        context.Response.Headers.Append("X-Robots-Tag", "noindex, nofollow, noarchive");
    }
    await next();
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

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(IdentityConstants.ApplicationScheme);

    if (identityOptions.EnableExternalIdentity)
    {
        await context.SignOutAsync(
            OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties
            {
                RedirectUri = identityOptions.SignedOutRedirectUri
            });
        return;
    }

    context.Response.Redirect("/login");
}).AllowAnonymous();

app.MapRazorPages();

// AD-0004: Blazor Web App route mapping. MapRazorComponents
// hosts the App root component (the HTML document) and serves every
// `@page` Razor component under it. AddInteractiveServerRenderMode
// wires the SignalR endpoint that components annotated with
// `@rendermode InteractiveServer` connect to; without that call,
// the annotation has no effect.
app.MapRazorComponents<TabFlow.Platform.Components.App>()
    .AddInteractiveServerRenderMode();

Log.Information("TabFlow Platform started successfully");
app.Run();
return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "TabFlow Platform terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// `public partial class Program` exposes the auto-generated entry
// point so test projects can target the host when they need an
// in-process ASP.NET Core factory.
public partial class Program { }
