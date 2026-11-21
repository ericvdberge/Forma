using Microsoft.CodeAnalysis;

namespace Forma.Analyzers;

/// <summary>The rules Forma's analyzers report. Both are errors: the build fails until the query goes through a filter.</summary>
public static class Diagnostics
{
    private const string Category = "Forma.Repositories";

    /// <summary>A query is narrowed with its own predicate instead of a <c>{Entity}Filter</c>.</summary>
    public static readonly DiagnosticDescriptor NoAdHocPredicate = new(
        id: "FORMA001",
        title: "Filter queries with ApplyFilter, not with Where or a predicate",
        messageFormat: "'{0}' narrows the query with its own predicate. Add the condition to the entity's filter and use ApplyFilter instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Database queries are narrowed only through a {Entity}Filter and its {Entity}QueryExtensions.ApplyFilter, so every read and delete goes through the same checks (CLAUDE.md, \"Repositories are CRUD only\"). Where and predicate overloads such as FirstOrDefaultAsync(x => …) or AnyAsync(x => …) are allowed only inside the *QueryExtensions classes that build those filters.");

    /// <summary>A repository runs a query on a table without going through <c>ApplyFilter</c>.</summary>
    public static readonly DiagnosticDescriptor MissingApplyFilter = new(
        id: "FORMA002",
        title: "Repository queries go through ApplyFilter",
        messageFormat: "This query on '{0}' doesn't go through ApplyFilter. Narrow it with {1}'s filter first (an empty filter reads everything).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every query a repository runs on a DbSet (ToListAsync, FirstOrDefaultAsync, AnyAsync, ExecuteDeleteAsync, Find…) is narrowed with ApplyFilter first, so an id from a request can't reach another website's data (CLAUDE.md, \"Filters guard against IDOR\"). Pass an empty filter to read everything.");
}
