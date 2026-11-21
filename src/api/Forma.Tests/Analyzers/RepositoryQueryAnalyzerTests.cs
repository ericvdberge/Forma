using Forma.Analyzers;

namespace Forma.Tests.Analyzers;

public class RepositoryQueryAnalyzerTests
{
    /// <summary>A small database layer in the shape of Forma's: a DAO, its filter and query extensions, and a context.</summary>
    private const string DatabaseLayer = """
        using System;
        using System.Linq;
        using System.Linq.Expressions;
        using System.Threading.Tasks;
        using Microsoft.EntityFrameworkCore;

        public class PageDao
        {
            public Guid Id { get; set; }
            public Guid WebsiteId { get; set; }
        }

        public class PageFilter
        {
            public Guid? Id { get; init; }
        }

        public static class QueryableExtensions
        {
            public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate) =>
                condition ? query.Where(predicate) : query;
        }

        internal static class PageQueryExtensions
        {
            public static IQueryable<PageDao> ApplyFilter(this IQueryable<PageDao> query, PageFilter filter) =>
                query.WhereIf(filter.Id.HasValue, p => p.Id == filter.Id);
        }

        public class FormaDbContext : DbContext
        {
            public DbSet<PageDao> Pages => Set<PageDao>();
        }
        """;

    private static Task<IReadOnlyList<(string Id, string Code)>> AnalyzeAsync(string code) =>
        AnalyzerRunner.RunAsync(new RepositoryQueryAnalyzer(), DatabaseLayer + code);

    [Fact]
    public async Task Accepts_queries_that_go_through_ApplyFilter()
    {
        var diagnostics = await AnalyzeAsync("""
            public class PageRepository(FormaDbContext db)
            {
                public async Task<object> ReadAsync(Guid id) =>
                    await db.Pages.AsNoTracking().ApplyFilter(new PageFilter { Id = id }).OrderBy(p => p.Id).ToListAsync();

                public async Task<PageDao?> FirstAsync(Guid id) =>
                    await db.Pages.ApplyFilter(new PageFilter { Id = id }).FirstOrDefaultAsync();

                public async Task<int> DeleteAsync(Guid id) =>
                    await db.Pages.ApplyFilter(new PageFilter { Id = id }).ExecuteDeleteAsync();

                public void Add(PageDao page) => db.Pages.Add(page);
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Flags_Where_and_predicate_overloads()
    {
        var diagnostics = await AnalyzeAsync("""
            public class PageRepository(FormaDbContext db)
            {
                public async Task<int> DeleteAsync(Guid id) =>
                    await db.Pages.ApplyFilter(new PageFilter()).Where(p => p.Id == id).ExecuteDeleteAsync();

                public async Task<bool> ExistsAsync(Guid id) =>
                    await db.Pages.ApplyFilter(new PageFilter()).AnyAsync(p => p.Id == id);

                public async Task<PageDao?> FirstAsync(Guid id) =>
                    await db.Pages.ApplyFilter(new PageFilter()).FirstOrDefaultAsync(p => p.Id == id);

                public async Task<object> QuerySyntaxAsync(Guid id) =>
                    await (from p in db.Pages.ApplyFilter(new PageFilter()) where p.Id == id select p).ToListAsync();
            }
            """);

        Assert.Equal(
            [
                ("FORMA001", "Where"),
                ("FORMA001", "AnyAsync"),
                ("FORMA001", "FirstOrDefaultAsync"),
                ("FORMA001", "where p.Id == id"),
            ],
            diagnostics);
    }

    [Fact]
    public async Task Flags_repository_queries_that_skip_ApplyFilter()
    {
        var diagnostics = await AnalyzeAsync("""
            public class PageRepository(FormaDbContext db)
            {
                public async Task<object> ReadAllAsync() =>
                    await db.Pages.AsNoTracking().OrderBy(p => p.Id).ToListAsync();

                public async Task<int> DeleteAllAsync() =>
                    await db.Pages.ExecuteDeleteAsync();

                public async Task<PageDao?> FindAsync(Guid id) =>
                    await db.Pages.FindAsync(id);
            }
            """);

        Assert.Equal(
            [
                ("FORMA002", "db.Pages"),
                ("FORMA002", "db.Pages"),
                ("FORMA002", "FindAsync"),
            ],
            diagnostics);
    }

    [Fact]
    public async Task Allows_predicates_in_query_extensions_and_leaves_lists_alone()
    {
        // DatabaseLayer itself calls Where in QueryableExtensions and WhereIf in PageQueryExtensions.
        var diagnostics = await AnalyzeAsync("""
            public class PageService
            {
                public object Published(PageDao[] pages, Guid websiteId) =>
                    pages.Where(p => p.WebsiteId == websiteId).ToList();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Flags_predicates_outside_repositories_too()
    {
        var diagnostics = await AnalyzeAsync("""
            public class PageSeeder(FormaDbContext db)
            {
                public async Task<bool> HasPagesAsync(Guid websiteId) =>
                    await db.Pages.AnyAsync(p => p.WebsiteId == websiteId);
            }
            """);

        Assert.Equal([("FORMA001", "AnyAsync")], diagnostics);
    }
}
