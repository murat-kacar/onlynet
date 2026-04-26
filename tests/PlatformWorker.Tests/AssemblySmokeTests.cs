using FluentAssertions;
using TabFlow.PlatformWorker;
using Xunit;

namespace PlatformWorker.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void PlatformWorkerAssembly_IsReferenced()
    {
        typeof(ProvisioningWorker).Assembly.GetName().Name.Should().Be("TabFlow.PlatformWorker");
    }
}
