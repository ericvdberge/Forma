using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Forma.Analyzers;

/// <summary>
/// Keeps database queries on <c>ApplyFilter</c>:
/// <list type="bullet">
/// <item><b>FORMA001</b>: no <c>Where</c> or predicate overloads on an <c>IQueryable</c>, except in the
/// <c>*QueryExtensions</c> classes that build the filters.</item>
/// <item><b>FORMA002</b>: in a <c>*Repository</c> class, a query that starts at a <c>DbSet</c> calls <c>ApplyFilter</c>
/// before it runs.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepositoryQueryAnalyzer : DiagnosticAnalyzer
{
    private const string ApplyFilter = "ApplyFilter";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.NoAdHocPredicate, Diagnostics.MissingApplyFilter);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var types = KnownTypes.From(context.Compilation);

        if (types is null)
            return;

        context.RegisterOperationAction(c => AnalyzeInvocation(c, types), OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, KnownTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var containingType = context.ContainingSymbol.ContainingType;

        if (containingType is null)
            return;

        if (!IsFilterBuilder(containingType) && HasAdHocPredicate(invocation, types))
            Report(context, Diagnostics.NoAdHocPredicate, MethodNameLocation(invocation), invocation.TargetMethod.Name);

        if (IsRepository(containingType))
            CheckRunsThroughApplyFilter(context, invocation, types);
    }

    /* ---------- FORMA001 ---------- */

    /// <summary>The <c>{Entity}QueryExtensions</c> classes (and <c>QueryableExtensions.WhereIf</c>) are where predicates belong.</summary>
    private static bool IsFilterBuilder(INamedTypeSymbol type) =>
        type.Name.EndsWith("QueryExtensions") || type.Name.EndsWith("QueryableExtensions");

    private static bool IsRepository(INamedTypeSymbol type) => type.Name.EndsWith("Repository");

    /// <summary>An operator on a query that takes a predicate: <c>Where</c>, <c>FirstOrDefaultAsync(x =&gt; …)</c>, <c>AnyAsync(x =&gt; …)</c>…</summary>
    private static bool HasAdHocPredicate(IInvocationOperation invocation, KnownTypes types)
    {
        if (QuerySource(invocation, types) is null)
            return false;

        return invocation.TargetMethod.Parameters.Skip(1).Any(p => types.IsPredicateExpression(p.Type));
    }

    /* ---------- FORMA002 ---------- */

    private static void CheckRunsThroughApplyFilter(OperationAnalysisContext context, IInvocationOperation invocation, KnownTypes types)
    {
        if (types.IsDbSetLookup(invocation))
        {
            Report(context, Diagnostics.MissingApplyFilter, MethodNameLocation(invocation), SetName(invocation.Instance!), EntityName(invocation.Instance!.Type!));
            return;
        }

        // Only the operator that runs the query (ToListAsync, AnyAsync, ExecuteDeleteAsync…) is checked, once per chain.
        if (QuerySource(invocation, types) is not { } source || types.IsQueryable(invocation.Type))
            return;

        var root = WalkToRoot(source, types, out var filtered);

        if (!filtered && root is not null && types.IsDbSet(root.Type))
            Report(context, Diagnostics.MissingApplyFilter, root.Syntax.GetLocation(), SetName(root), EntityName(root.Type!));
    }

    /// <summary>
    /// Follows a query back through its operators to where it starts (a <c>DbSet</c>, a parameter, a local), noting
    /// whether one of them is <c>ApplyFilter</c>.
    /// </summary>
    private static IOperation? WalkToRoot(IOperation query, KnownTypes types, out bool filtered)
    {
        filtered = false;
        var current = Unwrap(query);

        while (current is IInvocationOperation step && QuerySource(step, types) is { } previous)
        {
            if (step.TargetMethod.Name == ApplyFilter)
            {
                filtered = true;
                return current;
            }

            current = Unwrap(previous);
        }

        return current;
    }

    /// <summary>The query an operator works on: the receiver of an extension such as <c>Where</c> or <c>ToListAsync</c>.</summary>
    private static IOperation? QuerySource(IInvocationOperation invocation, KnownTypes types)
    {
        var method = invocation.TargetMethod;

        if (!method.IsExtensionMethod || invocation.Arguments.Length == 0)
            return null;

        var source = invocation.Arguments[0].Value;

        return types.IsQueryable(Unwrap(source).Type) ? source : null;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
            operation = conversion.Operand;

        return operation;
    }

    private static string SetName(IOperation set) => set switch
    {
        IPropertyReferenceOperation property => property.Property.Name,
        IFieldReferenceOperation field => field.Field.Name,
        _ => set.Syntax.ToString(),
    };

    /// <summary><c>DbSet&lt;PageDao&gt;</c> → <c>PageDao</c>.</summary>
    private static string EntityName(ITypeSymbol setType) =>
        setType is INamedTypeSymbol { TypeArguments.Length: 1 } named ? named.TypeArguments[0].Name : setType.Name;

    /// <summary>Just the method's name (<c>Where</c> in <c>db.Pages.Where(…)</c>), so the squiggle doesn't cover the whole chain.</summary>
    private static Location MethodNameLocation(IInvocationOperation invocation) =>
        invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }
            ? member.Name.GetLocation()
            : invocation.Syntax.GetLocation();

    private static void Report(OperationAnalysisContext context, DiagnosticDescriptor rule, Location location, params object[] args) =>
        context.ReportDiagnostic(Diagnostic.Create(rule, location, args));

    /// <summary>The framework types the rules look for, resolved once per compilation.</summary>
    private sealed class KnownTypes(INamedTypeSymbol queryable, INamedTypeSymbol expression, INamedTypeSymbol? dbSet)
    {
        public static KnownTypes? From(Compilation compilation)
        {
            var queryable = compilation.GetTypeByMetadataName("System.Linq.IQueryable");
            var expression = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
            var dbSet = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");

            return queryable is null || expression is null ? null : new KnownTypes(queryable, expression, dbSet);
        }

        public bool IsQueryable(ITypeSymbol? type) =>
            type is not null && (SymbolEqualityComparer.Default.Equals(type, queryable) || type.AllInterfaces.Contains(queryable, SymbolEqualityComparer.Default));

        public bool IsDbSet(ITypeSymbol? type)
        {
            for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, dbSet))
                    return true;
            }

            return false;
        }

        /// <summary><c>Expression&lt;Func&lt;…, bool&gt;&gt;</c>: a condition the database evaluates.</summary>
        public bool IsPredicateExpression(ITypeSymbol type) =>
            type is INamedTypeSymbol { TypeArguments.Length: 1 } named
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, expression)
            && named.TypeArguments[0] is INamedTypeSymbol { Name: "Func" } func
            && func.TypeArguments.Last().SpecialType == SpecialType.System_Boolean;

        /// <summary><c>db.Pages.Find(id)</c> and <c>FindAsync</c>: reads that skip the query and its filter.</summary>
        public bool IsDbSetLookup(IInvocationOperation invocation) =>
            invocation.TargetMethod.Name is "Find" or "FindAsync"
            && invocation.Instance is { } instance
            && IsDbSet(instance.Type);
    }
}
