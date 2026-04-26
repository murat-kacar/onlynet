using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TabFlow.Analyzers;

/// <summary>
/// Enforces AC-133 ("No test in the <c>Unit</c> tier MAY touch the file
/// system, the network, the system clock, or a database") by flagging
/// any identifier in a class carrying <c>[Trait("Category", "Unit")]</c>
/// that resolves to a forbidden type or member.
///
/// The Unit-tier definition lives in
/// <c>/doc/docs/explanation/concepts/test-taxonomy.md</c>. The forbidden
/// surface — `Npgsql.*` (database), `HttpClient`, `System.Net.Sockets.*`
/// (network), `System.IO.File`/`Directory`/`FileStream` (file system),
/// `DateTime.Now`, `DateTimeOffset.Now` (system clock) — is the
/// minimum signal the audit pass at
/// <c>/doc/buildlog/code-audit-2026-04-25.md</c> identified as
/// rendering the unit/integration distinction enforceable.
///
/// Tests that genuinely need any of those move to the Integration
/// tier (`[Trait("Category", "Integration")]`); the rule is silent
/// outside the Unit tier.
///
/// Diagnostic ID `TF0002` follows the same `TF` prefix convention as
/// `TF0001` and is recorded in
/// <c>tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnitTierTestPurityAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TF0002";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Unit-tier test references a forbidden type or member",
        messageFormat: "Unit-tier test '{0}' references '{1}'; AC-133 forbids file system, network, system clock, and database access in the Unit tier",
        category: "Testing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Per AC-133 in /doc/docs/explanation/concepts/test-taxonomy.md, " +
            "classes carrying [Trait(\"Category\", \"Unit\")] must not " +
            "import or reference Npgsql, HttpClient, System.Net.Sockets.*, " +
            "System.IO File/Directory/FileStream, or DateTime.Now / " +
            "DateTimeOffset.Now. Move tests that need these to the " +
            "Integration tier.",
        helpLinkUri:
            "https://github.com/onlynet/tabflow/blob/main/doc/docs/explanation/concepts/test-taxonomy.md");

    // Namespaces whose every type is forbidden in Unit-tier test code.
    private static readonly string[] BannedNamespaces =
    {
        "Npgsql",
        "System.Net.Sockets",
    };

    // Specific fully-qualified types that are forbidden even though
    // their namespace contains other allowed types.
    private static readonly string[] BannedTypes =
    {
        "System.Net.Http.HttpClient",
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileStream",
    };

    // (Type, Member) pairs that are forbidden as static accesses; the
    // member is the property name. Other members on the same type
    // remain allowed (e.g. DateTime.UtcNow is fine but DateTime.Now is
    // not, because Now is locale-bound and therefore non-deterministic).
    private static readonly (string Type, string Member)[] BannedMembers =
    {
        ("System.DateTime", "Now"),
        ("System.DateTimeOffset", "Now"),
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeIdentifier,
            SyntaxKind.IdentifierName);
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        var node = (IdentifierNameSyntax)context.Node;

        // Skip identifiers inside `using` directives, namespace
        // declarations, and similar contexts where the identifier is
        // not a code reference. Without this, every `using Xunit;`
        // statement would be visited.
        if (node.FirstAncestorOrSelf<UsingDirectiveSyntax>() is not null)
        {
            return;
        }

        // The contextual `var` keyword shows up as an IdentifierName
        // whose semantic symbol is the inferred type. Without this
        // skip, `var client = new HttpClient();` reports TF0002
        // twice — once on `HttpClient` and once on `var`. The
        // `var`-side report has no actionable user identifier
        // (the user did not type `HttpClient` there) and is dropped.
        if (node.Identifier.ValueText == "var" &&
            node.Parent is VariableDeclarationSyntax)
        {
            return;
        }

        var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDecl is null)
        {
            return;
        }

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol is null)
        {
            return;
        }

        if (!HasUnitTrait(classSymbol))
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(node);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        if (symbol is null)
        {
            return;
        }

        // Static-property accesses (DateTime.Now / DateTimeOffset.Now)
        // surface as IPropertySymbol whose ContainingType matches the
        // banned (Type, Member) pair. Check this before the namespace /
        // type rules because Now is allowed on most types — only on
        // DateTime / DateTimeOffset is it the system clock.
        if (symbol is IPropertySymbol property && property.IsStatic)
        {
            var ownerName = property.ContainingType.ToDisplayString();
            foreach (var (Type, Member) in BannedMembers)
            {
                if (ownerName == Type && property.Name == Member)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        node.GetLocation(),
                        classSymbol.Name,
                        $"{ownerName}.{Member}"));
                    return;
                }
            }
        }

        // Only report on direct type references (e.g. `File`,
        // `HttpClient`, `NpgsqlConnection`). Member accesses
        // (`File.ReadAllBytes(...)`) emit a second `IMethodSymbol` /
        // `IPropertySymbol` / `IFieldSymbol` identifier on the
        // member name; reporting both produces two diagnostics for
        // one offending statement. The type-side identifier is
        // sufficient to surface the violation, and the location is
        // closer to the import statement the user must remove.
        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        var fullName = typeSymbol.ToDisplayString();
        var ns = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        var hit =
            BannedNamespaces.Any(b => ns == b || ns.StartsWith(b + ".", System.StringComparison.Ordinal)) ||
            BannedTypes.Contains(fullName);

        if (hit)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                node.GetLocation(),
                classSymbol.Name,
                fullName));
        }
    }

    private static bool HasUnitTrait(INamedTypeSymbol classSymbol)
    {
        foreach (var attr in classSymbol.GetAttributes())
        {
            // xUnit's [Trait(name, value)] takes two string arguments.
            // We compare on the attribute's declared type name to
            // tolerate any future change in the assembly-qualified
            // name (e.g. xUnit v3 namespace move).
            var attrName = attr.AttributeClass?.Name;
            if (attrName != "TraitAttribute")
            {
                continue;
            }

            if (attr.ConstructorArguments.Length < 2)
            {
                continue;
            }

            var key = attr.ConstructorArguments[0].Value as string;
            var value = attr.ConstructorArguments[1].Value as string;
            if (key == "Category" && value == "Unit")
            {
                return true;
            }
        }

        return false;
    }
}
