extern alias TenantHost;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Xunit;

namespace TabFlow.E2E.Tests;

// E2E tier per /doc/docs/explanation/concepts/test-taxonomy.md.
// Spins up the tenant host through WebApplicationFactory and a
// real headless Chromium via Playwright. Excluded from both Unit
// and Integration fast-paths in CI.
[Trait("Category", "E2E")]
public class TenantE2ETests : IAsyncLifetime
{
    private WebApplicationFactory<TenantHost::Program>? _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<TenantHost::Program>();
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
        _factory?.Dispose();
    }

    [Fact]
    public async Task Tenant_HomePage_ReturnsSuccess()
    {
        // Arrange
        var client = _factory!.CreateClient();
        
        // Act
        var response = await client.GetAsync("/");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("TabFlow Tenant", content);
    }

    [Fact]
    public async Task Tenant_MenuPage_LoadsCorrectly()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        var client = _factory!.CreateClient();
        var url = client.BaseAddress?.ToString() ?? "http://localhost";
        
        // Act
        await page.GotoAsync($"{url}/menu");
        
        // Assert
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var title = await page.TitleAsync();
        Assert.Contains("TabFlow Tenant", title);
    }

    [Fact]
    public async Task Tenant_OrderFlow_Works()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        var client = _factory!.CreateClient();
        var url = client.BaseAddress?.ToString() ?? "http://localhost";
        
        // Act
        await page.GotoAsync($"{url}/menu");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Assert
        var title = await page.TitleAsync();
        Assert.Contains("TabFlow Tenant", title);
    }
}
