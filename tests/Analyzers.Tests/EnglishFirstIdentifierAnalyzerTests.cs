using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using TabFlow.Analyzers;
using Xunit;

namespace TabFlow.Analyzers.Tests;

/// <summary>
/// Regression tests for <see cref="EnglishFirstIdentifierAnalyzer"/>
/// (TF0001). Each test feeds a string of C# source into the
/// `CSharpAnalyzerTest` harness and asserts which diagnostics, if
/// any, the analyser produces.
///
/// Per the test taxonomy at
/// <c>/doc/docs/explanation/concepts/test-taxonomy.md</c> these tests
/// live in the Unit tier: they exercise one production class, run in
/// the test process, and never touch the file system, the network, or
/// the system clock. They close TD-0009 step 5.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EnglishFirstIdentifierAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostic_WhenAllIdentifiersAreAscii()
    {
        const string source = """
            using System;

            namespace TabFlow.Sample;

            public sealed class CustomerSession
            {
                public Guid Id { get; set; }
                public string TableLabel { get; set; } = "T-12";

                public void Open(int seatCount) { }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0001_OnNamedType_WhenContainsNonAscii()
    {
        const string source = """
            namespace TabFlow.Sample;

            public sealed class {|TF0001:Müşteri|}
            {
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0001_OnMethod_WhenContainsNonAscii()
    {
        const string source = """
            namespace TabFlow.Sample;

            public static class Utility
            {
                public static void {|TF0001:İşlemBaşlat|}() { }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0001_OnProperty_WhenContainsNonAscii()
    {
        const string source = """
            namespace TabFlow.Sample;

            public sealed class Order
            {
                public string {|TF0001:İsim|} { get; set; } = string.Empty;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0001_OnField_WhenContainsNonAscii()
    {
        const string source = """
            namespace TabFlow.Sample;

            public sealed class CartItem
            {
                public string {|TF0001:_müşteriAdı|} = string.Empty;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TF0001_OnParameter_WhenContainsNonAscii()
    {
        const string source = """
            namespace TabFlow.Sample;

            public static class Util
            {
                public static void Compute(int {|TF0001:sayı|}) { }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_OnCompilerGeneratedNames()
    {
        // The analyser skips identifiers starting with '<' or '$' so
        // backing fields, anonymous types, lambda closures, and other
        // generated identifiers do not surface as user-visible
        // diagnostics. This case exercises a property declaration
        // whose backing field is named `<Name>k__BackingField` by the
        // compiler; the user-visible identifier `Name` is ASCII and
        // emits no diagnostic.
        const string source = """
            namespace TabFlow.Sample;

            public sealed class Person
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    private static async Task VerifyAnalyzerAsync(string source)
    {
        var test = new CSharpAnalyzerTest<EnglishFirstIdentifierAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        await test.RunAsync();
    }
}
