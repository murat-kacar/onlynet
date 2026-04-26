using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TabFlow.Analyzers;

/// <summary>
/// Enforces TD-0022's design rule: ASP.NET Core controllers (classes
/// derived from <c>Microsoft.AspNetCore.Mvc.ControllerBase</c>) must
/// not hold an EF Core <c>DbContext</c> as a field, property, or
/// constructor parameter. Reads and writes belong in the application
/// service layer (AD-0003 trade-off: "host → application service →
/// domain"); a controller that owns a `DbContext` couples the HTTP
/// transport surface to the EF Core query shape.
///
/// The rule fires on:
///   - any field whose type derives from `DbContext` on a controller,
///   - any constructor parameter whose type derives from `DbContext`
///     on a controller.
///
/// Diagnostic ID `TF0003`. Documented in
/// <c>tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ControllerDbContextAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TF0003";

    private const string ControllerBaseFullName = "Microsoft.AspNetCore.Mvc.ControllerBase";
    private const string DbContextFullName = "Microsoft.EntityFrameworkCore.DbContext";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Controller depends on DbContext directly",
        messageFormat: "Controller '{0}' depends on '{1}'; route reads and writes through an application service per AD-0003 / TD-0022",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "Per AD-0003's host→application service→domain trade-off, " +
            "and per TD-0022, classes derived from " +
            "Microsoft.AspNetCore.Mvc.ControllerBase must not hold an " +
            "EF Core DbContext as a field, property, or constructor " +
            "parameter. Move the read or write into an application " +
            "service in the corresponding host's Services/ folder and " +
            "have the controller depend on the service interface " +
            "instead.",
        helpLinkUri:
            "https://github.com/onlynet/tabflow/blob/main/doc/docs/reference/architecture/decisions.md#ad-0003-one-host-process-per-side");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (!InheritsFrom(type, ControllerBaseFullName))
        {
            return;
        }

        // Field / property dependencies.
        foreach (var member in type.GetMembers())
        {
            switch (member)
            {
                case IFieldSymbol field when InheritsFrom(field.Type, DbContextFullName):
                    // Skip the compiler-generated backing field of an
                    // auto-property; the property itself reports the
                    // diagnostic in the next case branch and counting
                    // both produces two diagnostics for one decl.
                    if (field.IsImplicitlyDeclared || field.AssociatedSymbol is IPropertySymbol)
                    {
                        break;
                    }
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        field.Locations.FirstOrDefault() ?? Location.None,
                        type.Name,
                        field.Type.ToDisplayString()));
                    break;

                case IPropertySymbol property when InheritsFrom(property.Type, DbContextFullName):
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        property.Locations.FirstOrDefault() ?? Location.None,
                        type.Name,
                        property.Type.ToDisplayString()));
                    break;
            }
        }

        // Constructor-parameter dependencies. The constructor is the
        // primary injection point for ASP.NET Core controllers; flagging
        // it surfaces the violation at the hand-edit site, not on the
        // generated backing field.
        foreach (var ctor in type.InstanceConstructors)
        {
            foreach (var parameter in ctor.Parameters)
            {
                if (InheritsFrom(parameter.Type, DbContextFullName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        parameter.Locations.FirstOrDefault() ?? Location.None,
                        type.Name,
                        parameter.Type.ToDisplayString()));
                }
            }
        }
    }

    private static bool InheritsFrom(ITypeSymbol type, string baseTypeFullName)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == baseTypeFullName)
            {
                return true;
            }
        }

        return false;
    }
}
