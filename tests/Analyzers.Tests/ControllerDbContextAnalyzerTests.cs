using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using TabFlow.Analyzers;
using Xunit;

namespace TabFlow.Analyzers.Tests;

/// <summary>
/// Regression tests for <see cref="ControllerDbContextAnalyzer"/>
/// (TF0003). The harness compiles a synthetic C# source string,
/// runs the analyser, and asserts which diagnostics fire.
///
/// Per the test taxonomy these tests live in the Unit tier (no DB,
/// no file system, no network). They close the regression-test
/// half of TD-0022 step 4.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ControllerDbContextAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_OnControllerWithServiceDependency()
    {
        const string source = """
            using Microsoft.AspNetCore.Mvc;

            namespace TabFlow.Sample;

            public interface IThingService { }

            public sealed class ThingsController : ControllerBase
            {
                private readonly IThingService _service;

                public ThingsController(IThingService service)
                {
                    _service = service;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0003_OnControllerWithDbContextField()
    {
        const string source = """
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.EntityFrameworkCore;

            namespace TabFlow.Sample;

            public sealed class SampleDbContext : DbContext { }

            public sealed class ThingsController : ControllerBase
            {
                private readonly SampleDbContext {|TF0003:_context|};

                public ThingsController(SampleDbContext {|TF0003:context|})
                {
                    _context = context;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0003_OnControllerWithDbContextProperty()
    {
        const string source = """
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.EntityFrameworkCore;

            namespace TabFlow.Sample;

            public sealed class SampleDbContext : DbContext { }

            public sealed class ThingsController : ControllerBase
            {
                public SampleDbContext {|TF0003:Context|} { get; }

                public ThingsController(SampleDbContext {|TF0003:context|})
                {
                    Context = context;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_OnNonControllerClassWithDbContext()
    {
        const string source = """
            using Microsoft.EntityFrameworkCore;

            namespace TabFlow.Sample;

            public sealed class SampleDbContext : DbContext { }

            public sealed class ThingsService
            {
                private readonly SampleDbContext _context;

                public ThingsService(SampleDbContext context)
                {
                    _context = context;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    private static async Task VerifyAnalyzerAsync(string source)
    {
        var test = new CSharpAnalyzerTest<ControllerDbContextAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
                .AddPackages(System.Collections.Immutable.ImmutableArray.Create(
                    new PackageIdentity("Microsoft.AspNetCore.Mvc.Core", "2.3.0"),
                    new PackageIdentity("Microsoft.EntityFrameworkCore", "8.0.0"))),
        };

        await test.RunAsync();
    }
}
