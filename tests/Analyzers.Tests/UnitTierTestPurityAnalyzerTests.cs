using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using TabFlow.Analyzers;
using Xunit;

namespace TabFlow.Analyzers.Tests;

/// <summary>
/// Regression tests for <see cref="UnitTierTestPurityAnalyzer"/>
/// (TF0002). The harness compiles a synthetic C# source string,
/// runs the analyser, and asserts which diagnostics fire. The rule
/// is scoped: it fires only inside classes that carry the xUnit
/// trait <c>[Trait("Category", "Unit")]</c>.
///
/// Per the test taxonomy at
/// <c>/doc/docs/explanation/concepts/test-taxonomy.md</c> these
/// tests live in the Unit tier (no DB, no file system, no clock).
/// </summary>
[Trait("Category", "Unit")]
public sealed class UnitTierTestPurityAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenUnitTierUsesAllowedTypesOnly()
    {
        const string source = """
            using System;
            using Xunit;

            namespace TabFlow.Sample;

            [Trait("Category", "Unit")]
            public sealed class PureUnitTests
            {
                [Fact]
                public void DoesNotTouchTheWorld()
                {
                    var stamp = DateTime.UtcNow;
                    var span = TimeSpan.FromMilliseconds(1);
                    _ = stamp;
                    _ = span;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0002_OnDateTimeNow_FromUnitTier()
    {
        const string source = """
            using System;
            using Xunit;

            namespace TabFlow.Sample;

            [Trait("Category", "Unit")]
            public sealed class ClockTouchingTests
            {
                [Fact]
                public void ReadsLocalNow()
                {
                    var stamp = DateTime.{|TF0002:Now|};
                    _ = stamp;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0002_OnDateTimeOffsetNow_FromUnitTier()
    {
        const string source = """
            using System;
            using Xunit;

            namespace TabFlow.Sample;

            [Trait("Category", "Unit")]
            public sealed class OffsetClockTouchingTests
            {
                [Fact]
                public void ReadsLocalNow()
                {
                    var stamp = DateTimeOffset.{|TF0002:Now|};
                    _ = stamp;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0002_OnFileStaticAccess_FromUnitTier()
    {
        const string source = """
            using System.IO;
            using Xunit;

            namespace TabFlow.Sample;

            [Trait("Category", "Unit")]
            public sealed class FileTouchingTests
            {
                [Fact]
                public void ReadsAFile()
                {
                    var bytes = {|TF0002:File|}.ReadAllBytes("/tmp/x");
                    _ = bytes;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0002_OnHttpClientType_FromUnitTier()
    {
        const string source = """
            using System.Net.Http;
            using Xunit;

            namespace TabFlow.Sample;

            [Trait("Category", "Unit")]
            public sealed class NetworkTouchingTests
            {
                [Fact]
                public void TalksToTheNetwork()
                {
                    using var client = new {|TF0002:HttpClient|}();
                    _ = client;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenIntegrationTierUsesForbiddenType()
    {
        const string source = """
            using System;
            using System.IO;
            using Xunit;

            namespace TabFlow.Sample;

            [Trait("Category", "Integration")]
            public sealed class DatabaseBackedTests
            {
                [Fact]
                public void ReadsTheLocalNow()
                {
                    var stamp = DateTime.Now;
                    var bytes = File.ReadAllBytes("/tmp/x");
                    _ = stamp;
                    _ = bytes;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenClassCarriesNoTrait()
    {
        const string source = """
            using System;
            using Xunit;

            namespace TabFlow.Sample;

            public sealed class UntaggedTests
            {
                [Fact]
                public void ReadsLocalNow()
                {
                    var stamp = DateTime.Now;
                    _ = stamp;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    private static async Task VerifyAnalyzerAsync(string source)
    {
        var test = new CSharpAnalyzerTest<UnitTierTestPurityAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
                .AddPackages(System.Collections.Immutable.ImmutableArray.Create(
                    new PackageIdentity("xunit.core", "2.9.3"))),
        };

        await test.RunAsync();
    }
}
