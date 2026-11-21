using Forma.Analyzers;

namespace Forma.Tests.Analyzers;

public class RepositoryShapeAnalyzerTests
{
    private const string Entities = """
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;

        public class PageDao
        {
            public Guid Id { get; set; }
        }

        public class PageFilter
        {
            public Guid? Id { get; init; }
        }
        """;

    private static Task<IReadOnlyList<(string Id, string Code)>> AnalyzeAsync(string code) =>
        AnalyzerRunner.RunAsync(new RepositoryShapeAnalyzer(), Entities + code);

    [Fact]
    public async Task Accepts_crud_repositories()
    {
        var diagnostics = await AnalyzeAsync("""
            public interface IPageRepository
            {
                Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter);
                Task AddAsync(PageDao page);
                Task UpdateAsync(PageDao page);
                Task<bool> DeleteAsync(Guid id);
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Flags_single_row_reads_and_other_methods()
    {
        var diagnostics = await AnalyzeAsync("""
            public interface IPageRepository
            {
                Task<PageDao?> GetByIdAsync(Guid id);
                Task<PageDao?> GetPageAsync(PageFilter filter);
                Task<IReadOnlyList<PageDao>> GetAllAsync();
                Task<bool> ExistsAsync(PageFilter filter);
                Task SaveDraftAsync(PageDao page);
            }
            """);

        Assert.Equal(
            [
                ("FORMA003", "GetByIdAsync"),
                ("FORMA003", "GetPageAsync"),
                ("FORMA003", "GetAllAsync"),
                ("FORMA003", "ExistsAsync"),
                ("FORMA003", "SaveDraftAsync"),
            ],
            diagnostics);
    }

    [Fact]
    public async Task Leaves_core_services_and_private_helpers_alone()
    {
        var diagnostics = await AnalyzeAsync("""
            public class PageCoreService
            {
                public Task<PageDao?> GetPageAsync(PageFilter filter) => Task.FromResult<PageDao?>(null);
            }

            public class PageRepository
            {
                public Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter) => Task.FromResult<IReadOnlyList<PageDao>>([]);
                private Task<PageDao?> FindAsync(Guid id) => Task.FromResult<PageDao?>(null);
            }
            """);

        Assert.Empty(diagnostics);
    }
}
