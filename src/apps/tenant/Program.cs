using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TabFlow.Shared.Application.EventBus;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;
using TabFlow.Tenant.Services;
using TabFlow.Tenant.WebSocket;
using TabFlow.Tenant.Hubs;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

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

app.MapHub<TenantHub>("/hub/tenant");

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
