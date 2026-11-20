using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.PageContents;

public interface IPageContentRepository
{
    Task<IReadOnlyList<PageContentDao>> GetPageContentsAsync(PageContentFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(PageContentDao content, CancellationToken cancellationToken = default);

    Task UpdateAsync(PageContentDao content, CancellationToken cancellationToken = default);

    /// <summary>Deletes the page's content. Returns <c>false</c> when it has none.</summary>
    Task<bool> DeleteAsync(Guid pageId, CancellationToken cancellationToken = default);
}
