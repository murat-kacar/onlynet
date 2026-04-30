using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Xunit;

namespace Tenant.Tests.Controllers;

/// <summary>
/// Custom WebApplicationFactory that configures the tenant host for
/// integration testing. Overrides configuration to provide a test
/// connection string and disables file logging (the test environment
/// may not have /var/log/tabflow).
/// </summary>
public sealed class TenantWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TenantDb"] = "Host=localhost;Port=5432;Database=tabflow_tenant_test;Username=postgres;Password=postgres",
                ["TABFLOW_TENANT_CODE"] = "TEST-TENANT"
            });
        });

        // Disable Serilog file output for tests
        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Override Serilog configuration for tests to use console only
        // This prevents file logging errors when /var/log/tabflow doesn't exist
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.UseSerilog();
        return base.CreateHost(builder);
    }
}

/// <summary>
/// Integration-tier tests for the staff-vs-customer authorisation
/// boundary on the tenant host (TD-0015 step 6). Each test boots
/// the full ASP.NET Core pipeline via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>; the cookie auth
/// handler short-circuits the default 302 redirect to <c>/login</c>
/// for any path under <c>/api/</c> and returns the canonical
/// 401 / 403 status codes the cookie-config block in
/// <c>Program.cs</c> declares.
///
/// The tests do not exercise the database; the authorisation
/// pipeline runs before the controller action does, so a missing
/// or insufficient cookie produces the status code without ever
/// opening a SQL connection.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AuthEnforcementTests : IClassFixture<TenantWebApplicationFactory>
{
    private readonly TenantWebApplicationFactory _factory;

    public AuthEnforcementTests(TenantWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Staff read endpoint: no cookie → 401. Confirms the
    /// default-restrictive policy on
    /// <c>OrdersController</c> (Tenant:Read) plus the cookie
    /// handler's 401-on-missing-cookie short-circuit.
    /// </summary>
    [Fact]
    public async Task StaffOrders_GetById_NoCookie_Returns401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync(new Uri($"/api/orders/{Guid.NewGuid()}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Staff write endpoint: no cookie → 401. The session-close
    /// action is the staff-tier slice of <c>SessionsController</c>;
    /// per AC-043 it requires Tenant:Write while the rest of the
    /// controller's customer-tier actions opt out via
    /// <c>[AllowAnonymous]</c>.
    /// </summary>
    [Fact]
    public async Task StaffSessions_Close_NoCookie_Returns401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.PostAsync(
            new Uri($"/api/sessions/{Guid.NewGuid()}/close", UriKind.Relative),
            content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Staff read endpoint: no cookie → 401. Confirms that the
    /// `Tenant:Read` policy on `KitchenController.GetKitchenOrders`
    /// (action-level after the TD-0022 refactor) keeps the same
    /// 401-on-missing-cookie behaviour the class-level policy used
    /// to give it.
    /// </summary>
    [Fact]
    public async Task StaffKitchen_Orders_NoCookie_Returns401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync(new Uri("/api/kitchen/orders", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Customer-tier shim: no cookie → **not 401**. The
    /// `/api/public/*` prefix opts the controller out via
    /// `[AllowAnonymous]` (TD-0021 step 1), so the request reaches
    /// the controller action and fails on the application gate
    /// (missing device cookie, missing checkout-proof token, etc.).
    /// The expected response is 4xx but **not 401**: 401 means the
    /// authorisation pipeline rejected the request before the
    /// controller ran, which would mean the customer surface is
    /// gated by Identity — exactly what AD-0003 forbids.
    /// </summary>
    [Fact]
    public async Task PublicOrders_Submit_NoCookie_DoesNotReturn401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var dummyRequest = new
        {
            SessionId = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            TableId = Guid.NewGuid(),
            CheckoutProofToken = "no-such-token",
            IdempotencyKey = Guid.NewGuid().ToString(),
            Note = (string?)null,
        };
        var response = await client.PostAsJsonAsync(
            new Uri("/api/public/orders", UriKind.Relative),
            dummyRequest);

        // The customer-tier surface MUST NOT gate on Identity per
        // AD-0003 — a 401 here would mean the cookie-auth handler
        // rejected the request before the controller ran. The
        // application gate (PublicOrdersController.SubmitOrder
        // calls Forbid() when the device cookie is missing per
        // TD-0017) returns 403 today; that is the expected
        // outcome.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "the customer-tier surface MUST NOT gate on Identity per AD-0003");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "the application gate (missing device cookie) returns 403 per TD-0017");
    }

    [Fact]
    public async Task PublicCart_AddItem_NoCookie_Returns403()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var dummyRequest = new
        {
            SessionId = Guid.NewGuid(),
            MenuItemId = Guid.NewGuid(),
            Quantity = 1,
            Note = (string?)null,
        };
        var response = await client.PostAsJsonAsync(
            new Uri("/api/public/cart", UriKind.Relative),
            dummyRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "customer cart mutation requires the access-ticket cookie but not ASP.NET Identity");
    }
}
