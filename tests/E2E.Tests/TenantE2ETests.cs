using Microsoft.Playwright;
using Xunit;

namespace TabFlow.E2E.Tests;

// E2E tier per /doc/docs/explanation/concepts/test-taxonomy.md.
// Spins up the tenant host on a real loopback Kestrel port and a
// real headless Chromium via Playwright. Excluded from both Unit
// and Integration fast-paths in CI.
[Trait("Category", "E2E")]
public class TenantE2ETests : IAsyncLifetime
{
    private DotNetWebHost? _host;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        _host = await DotNetWebHost.StartAsync("src/apps/tenant/TabFlow.Tenant.csproj");
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await _browser?.CloseAsync()!;
        _playwright?.Dispose();
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }

    [Fact]
    public async Task Tenant_LoginPage_LoadsInBrowser()
    {
        // Arrange
        IPage page = await _browser!.NewPageAsync();

        // Act
        await page.GotoAsync(new Uri(_host!.BaseAddress, "/login").ToString());

        // Assert
        await page.WaitForSelectorAsync("h1");
        string? heading = await page.TextContentAsync("h1");
        Assert.Contains("Tenant Login", heading);
    }
}
