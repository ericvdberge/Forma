using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Forma.Analyzers;

/// <summary>
/// <b>FORMA003</b>: the public methods of a <c>*Repository</c> (class or interface) are CRUD only:
/// <list type="bullet">
/// <item><c>Get…Async({Entity}Filter filter, …)</c> returning <c>Task&lt;IReadOnlyList&lt;T&gt;&gt;</c>;</item>
/// <item><c>Add…</c>, <c>Update…</c> and <c>Delete…</c>.</item>
/// </list>
/// A single-row read such as <c>GetByIdAsync</c> or an <c>ExistsAsync</c> belongs in the core service, on top of the plural read.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepositoryShapeAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] WritePrefixes = ["Add", "Update", "Delete"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.RepositoryIsCrudOnly);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (!IsRepositoryMember(method))
            return;

        if (Problem(method) is not { } problem)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.RepositoryIsCrudOnly, method.Locations.FirstOrDefault(), method.Name, problem));
    }

    private static bool IsRepositoryMember(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary
        && method.DeclaredAccessibility == Accessibility.Public
        && method.ContainingType.Name.EndsWith("Repository");

    /// <summary>What is wrong with the method, or <c>null</c> when it is one of the CRUD shapes.</summary>
    private static string? Problem(IMethodSymbol method)
    {
        if (WritePrefixes.Any(prefix => method.Name.StartsWith(prefix)))
            return null;

        if (!method.Name.StartsWith("Get"))
            return "isn't CRUD. Repositories only have Get…Async(filter), Add…, Update… and Delete…: put lookups such as exists checks in the core service.";

        if (!TakesFilter(method))
            return "reads without a filter. Take a {Entity}Filter, so the core service narrows the read (Get{Entity}Async = the first match of the plural read).";

        if (!ReturnsList(method))
            return "reads a single row. Return a list (Task<IReadOnlyList<T>>) and let the core service take the first match.";

        return null;
    }

    private static bool TakesFilter(IMethodSymbol method) =>
        method.Parameters.FirstOrDefault() is { } first && first.Type.Name.EndsWith("Filter");

    /// <summary><c>Task&lt;IReadOnlyList&lt;T&gt;&gt;</c>.</summary>
    private static bool ReturnsList(IMethodSymbol method) =>
        method.ReturnType is INamedTypeSymbol { Name: "Task", TypeArguments.Length: 1 } task
        && task.TypeArguments[0] is INamedTypeSymbol { Name: "IReadOnlyList" };
}
