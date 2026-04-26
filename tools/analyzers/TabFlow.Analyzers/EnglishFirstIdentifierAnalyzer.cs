using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TabFlow.Analyzers;

/// <summary>
/// Enforces AD-0015 ("English-first for internal contracts") by
/// flagging any declared symbol whose name contains a non-ASCII
/// character. The rule covers types, methods, properties, fields,
/// events, and parameters; localised strings belong in resx files
/// routed through <c>IStringLocalizer&lt;T&gt;</c> per AC-119, not in
/// identifier names.
///
/// Scope: every C# project that references this analyser. The
/// canonical wiring is the <c>GlobalAnalyzers</c> ItemGroup in
/// <c>/Directory.Build.props</c>.
///
/// Diagnostic IDs use the <c>TF</c> prefix so they do not collide
/// with Roslyn's <c>CS</c>, the .NET analysers' <c>CA</c>, or the
/// IDE rule set's <c>IDE</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnglishFirstIdentifierAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TF0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Identifier contains non-ASCII characters",
        messageFormat: "Identifier '{0}' contains non-ASCII characters; AD-0015 requires English-first internal contracts per AC-117",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Per AD-0015, identifiers under /src/ and /tests/ must " +
            "contain ASCII letters, digits, and underscores only. " +
            "Localised user-facing strings belong in resx files " +
            "routed through IStringLocalizer<T> (AC-119), not in " +
            "identifier names.",
        helpLinkUri:
            "https://github.com/onlynet/tabflow/blob/main/doc/docs/explanation/concepts/internationalization.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(
            AnalyzeSymbol,
            SymbolKind.NamedType,
            SymbolKind.Method,
            SymbolKind.Property,
            SymbolKind.Field,
            SymbolKind.Event,
            SymbolKind.Parameter);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        var name = symbol.Name;

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        // Compiler-generated names (e.g. backing fields, anonymous
        // types, lambda closures) start with a character outside the
        // identifier alphabet ('<', '$') and are not user-authored;
        // skip them so the rule does not flag generated code.
        if (name[0] == '<' || name[0] == '$')
        {
            return;
        }

        // Property and event accessors (`get_X`, `set_X`, `add_X`,
        // `remove_X`) are compiler-synthesised methods whose name
        // mirrors the user-authored property or event. Reporting
        // them produces three diagnostics for one property declaration
        // (the property + the two accessors); skipping the accessors
        // keeps each non-ASCII identifier reported exactly once at
        // the user's declaration site.
        if (symbol is IMethodSymbol method &&
            (method.MethodKind == MethodKind.PropertyGet ||
             method.MethodKind == MethodKind.PropertySet ||
             method.MethodKind == MethodKind.EventAdd ||
             method.MethodKind == MethodKind.EventRemove ||
             method.MethodKind == MethodKind.EventRaise))
        {
            return;
        }

        // ASCII fast-path: scan once. Any code unit > 0x7F means a
        // non-ASCII character is present. A surrogate pair where the
        // high surrogate is < 0x80 is impossible, so this is safe
        // without dedicated UTF-16 handling.
        var hasNonAscii = false;
        foreach (var ch in name)
        {
            if (ch > 0x7F)
            {
                hasNonAscii = true;
                break;
            }
        }

        if (!hasNonAscii)
        {
            return;
        }

        var location = symbol.Locations.FirstOrDefault() ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, name));
    }
}
