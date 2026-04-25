using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TabFlow.Shared.Application.EventBus;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;
using TabFlow.Shared.Infrastructure.Diagnostics;
using TabFlow.Tenant.Services;
using TabFlow.Tenant.WebSocket;
using TabFlow.Tenant.Hubs;

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

builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TenantDb")));

builder.Services.AddSingleton<TableWebSocketHandler>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
builder.Services.AddScoped<ICustomerSessionService, CustomerSessionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddHostedService<EventSubscriptionService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<SignalRService>();

builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<TenantDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Tenant:Read", policy => policy.RequireClaim("TenantRole", "Read"));
    options.AddPolicy("Tenant:Write", policy => policy.RequireClaim("TenantRole", "Write"));
    options.AddPolicy("Tenant:Self", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("TabFlow.Tenant"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// Health checks per /doc/docs/reference/architecture/health-checks.md.
// /health/live carries no probes (liveness only). /health/ready runs
// the probe set tagged "ready". Additional probes (migration head,
// event-bus capacity, tenant-context) are tracked under TD-0013's
// payoff plan.
string[] readyTag = ["ready"];
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TenantDbContext>(
        name: "tenant-db:ping",
        tags: readyTag);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.Urls.Add("http://localhost:5001");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

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

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

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
