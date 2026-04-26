using FluentAssertions;
using System.IO;
using Xunit;

namespace Shared.Tests.Infrastructure;

/// <summary>
/// Regression test for TD-0026 step 3. Verifies that each host
/// (platform, platform-worker, tenant) calls
/// <c>builder.Host.UseSystemd()</c> (or <c>builder.Services.AddSystemd()</c>
/// for Generic Host) in its composition root. This is a guard against
/// a future refactor that accidentally removes the call, which would
/// break the <c>Type=notify</c> supervision contract documented in
/// <c>/doc/docs/how-to/supervise-processes.md</c>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UseSystemdCompositionRootTests
{
    [Fact]
    public void Platform_Program_CallsUseSystemd()
    {
        var programPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "src", "apps", "platform", "Program.cs");

        var programContent = File.ReadAllText(programPath);
        programContent.Should().Contain("UseSystemd()",
            "Platform Program.cs must call builder.Host.UseSystemd() per TD-0026");
    }

    [Fact]
    public void PlatformWorker_Program_CallsAddSystemd()
    {
        var programPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "src", "apps", "platform-worker", "Program.cs");

        var programContent = File.ReadAllText(programPath);
        programContent.Should().Contain("AddSystemd()",
            "PlatformWorker Program.cs must call builder.Services.AddSystemd() per TD-0026");
    }

    [Fact]
    public void Tenant_Program_CallsUseSystemd()
    {
        var programPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "src", "apps", "tenant", "Program.cs");

        var programContent = File.ReadAllText(programPath);
        programContent.Should().Contain("UseSystemd()",
            "Tenant Program.cs must call builder.Host.UseSystemd() per TD-0026");
    }
}
