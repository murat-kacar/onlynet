using FluentAssertions;
using Xunit;

namespace Platform.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void PlatformAssembly_IsReferenced()
    {
        typeof(global::Program).Assembly.GetName().Name.Should().Be("TabFlow.Platform");
    }
}
