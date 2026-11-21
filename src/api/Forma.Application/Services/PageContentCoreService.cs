using Forma.Application.Entities;
using Forma.Application.Mappers;
using Forma.Infrastructure.Repositories.PageContents;

namespace Forma.Application.Services;

/// <summary>Creates, reads, updates and deletes page content. Rules and workflows belong to the callers.</summary>
public interface IPageContentCoreService
{
    Task<IReadOnlyList<PageContent>> GetPageContentsAsync(PageContentFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The content matching <paramref name="filter"/>, or <c>null</c> when there is none.</summary>
    Task<PageContent?> GetPageContentAsync(PageContentFilter filter, CancellationToken cancellationToken = default);

    Task<PageContent> CreateAsync(PageContentInsert insert, CancellationToken cancellationToken = default);

    /// <summary>Applies <paramref name="update"/>, or returns <c>null</c> when the page has no content.</summary>
    Task<PageContent?> UpdateAsync(Guid pageId, PageContentUpdate update, CancellationToken cancellationToken = default);

    /// <summary>Deletes the page's content. Returns <c>false</c> when it has none.</summary>
    Task<bool> DeleteAsync(Guid pageId, CancellationToken cancellationToken = default);
}

public class PageContentCoreService(IPageContentRepository pageContentRepository, TimeProvider timeProvider) : IPageContentCoreService
{
    public async Task<IReadOnlyList<PageContent>> GetPageContentsAsync(PageContentFilter filter, CancellationToken cancellationToken = default)
    {
        var contents = await pageContentRepository.GetPageContentsAsync(filter, cancellationToken);
        return contents.ToEntities();
    }

    public async Task<PageContent?> GetPageContentAsync(PageContentFilter filter, CancellationToken cancellationToken = default)
    {
        var contents = await pageContentRepository.GetPageContentsAsync(filter, cancellationToken);
        return contents.FirstOrDefault()?.ToEntity();
    }

    public async Task<PageContent> CreateAsync(PageContentInsert insert, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var content = new PageContent
        {
            PageId = insert.PageId,
            Document = insert.Document,
            DraftDocument = insert.DraftDocument,
            DraftUpdatedAt = insert.DraftDocument is null ? null : now,
            PublishedAt = insert.PublishedAt,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await pageContentRepository.AddAsync(content.ToDao(), cancellationToken);

        return content;
    }

    public async Task<PageContent?> UpdateAsync(Guid pageId, PageContentUpdate update, CancellationToken cancellationToken = default)
    {
        var content = await GetPageContentAsync(new PageContentFilter { PageId = pageId }, cancellationToken);
        if (content is null)
            return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        content.Document = update.Document ?? content.Document;
        content.PublishedAt = update.PublishedAt ?? content.PublishedAt;
        content.UpdatedAt = now;

        if (update.DraftDocument is not null)
        {
            content.DraftDocument = update.DraftDocument;
            content.DraftUpdatedAt = now;
        }

        if (update.ClearDraft == true)
        {
            content.DraftDocument = null;
            content.DraftUpdatedAt = null;
        }

        await pageContentRepository.UpdateAsync(content.ToDao(), cancellationToken);

        return content;
    }

    public async Task<bool> DeleteAsync(Guid pageId, CancellationToken cancellationToken = default) =>
        await pageContentRepository.DeleteAsync(pageId, cancellationToken);
}
