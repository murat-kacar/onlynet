using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
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

builder.Services.AddScoped<IPlatformAuditService, PlatformAuditService>();
// TD-0022 step 1: read-path services that own the PlatformDbContext
// reads which used to live inline in the API controllers.
builder.Services.AddScoped<ITenantRegistryService, TenantRegistryService>();
builder.Services.AddScoped<IProvisioningJobReadService, ProvisioningJobReadService>();

builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<PlatformDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TabFlow.Platform"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// Health checks per /doc/docs/reference/architecture/health-checks.md.
// /health/live carries no probes (liveness only). /health/ready runs
// the probe set tagged "ready". Additional probes (migration head,
// worker heartbeat) are tracked under TD-0013's payoff plan.
string[] readyTag = ["ready"];
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PlatformDbContext>(
        name: "platform-db:ping",
        tags: readyTag);

// AD-0004 + TD-0027: Blazor Web App composition. AddRazorComponents
// registers the new component model (root component +
// MapRazorComponents<App>()); AddInteractiveServerComponents adds
// the SignalR-backed interactive server render mode that staff
// pages opt into via `@rendermode InteractiveServer`. AddRazorPages
// stays so Identity's Login.cshtml and ChangePassword.cshtml keep
// rendering as classic Razor Pages.
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<AuditMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// TD-0002 step 3: any authenticated principal carrying the
// must-change-password claim is bounced through /change-password
// until the claim is removed by ChangePasswordModel.OnPostAsync. The
// middleware sits after UseAuthorization so HttpContext.User is fully
// populated, and before the route mapping below so the redirect wins
// over the original handler.
app.UseMiddleware<PasswordChangeRequiredMiddleware>();

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

app.MapRazorPages();

// AD-0004 + TD-0027: Blazor Web App route mapping. MapRazorComponents
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
// point so test projects can target it via
// `WebApplicationFactory<Program>`. Required because the E2E.Tests
// project references both hosts; without an extern alias the two
// auto-generated `Program` classes collide on type name (CS0433).
public partial class Program { }
