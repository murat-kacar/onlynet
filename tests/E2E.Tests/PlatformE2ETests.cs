extern alias PlatformHost;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Xunit;

namespace TabFlow.E2E.Tests;

// E2E tier per /doc/docs/explanation/concepts/test-taxonomy.md.
// Spins up the platform host through WebApplicationFactory and a
// real headless Chromium via Playwright. Excluded from both Unit
// and Integration fast-paths in CI.
[Trait("Category", "E2E")]
public class PlatformE2ETests : IAsyncLifetime
{
    private WebApplicationFactory<PlatformHost::Program>? _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<PlatformHost::Program>();
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
    public async Task Platform_HomePage_ReturnsSuccess()
    {
        // Arrange
        var client = _factory!.CreateClient();
        
        // Act
        var response = await client.GetAsync("/");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("TabFlow Platform", content);
    }

    [Fact]
    public async Task Platform_Dashboard_LoadsCorrectly()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();
        var client = _factory!.CreateClient();
        var url = client.BaseAddress?.ToString() ?? "http://localhost";
        
        // Act
        await page.GotoAsync(url);
        
        // Assert
        await page.WaitForSelectorAsync("h1");
        var title = await page.TitleAsync();
        Assert.Contains("TabFlow Platform", title);
    }
}
