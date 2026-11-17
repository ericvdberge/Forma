using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Repositories.PageDocuments;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Services;

/// <summary>Reads and saves the Design Studio's page documents.</summary>
/// <remarks>Phase 2 adds <c>CheckAsync</c> and <c>PublishAsync</c>; see <c>docs/design/page-content.md</c>.</remarks>
public interface IPageContentCoreService
{
    /// <summary>The page's draft and published document, or null when the page does not exist.</summary>
    Task<PageContent?> GetContentAsync(Guid pageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the page's draft with <paramref name="document"/> when <paramref name="baseRevision"/> is the
    /// current draft revision. The document has passed structural validation in the API before this is called.
    /// </summary>
    Task<SaveDraftResult> SaveDraftAsync(Guid pageId, PageDocument document, int baseRevision, CancellationToken cancellationToken = default);
}

public class PageContentCoreService(
    IPageRepository pageRepository,
    IPageDocumentRepository pageDocumentRepository,
    TimeProvider timeProvider) : IPageContentCoreService
{
    public async Task<PageContent?> GetContentAsync(Guid pageId, CancellationToken cancellationToken = default)
    {
        if (await pageRepository.GetByIdAsync(pageId, cancellationToken) is null)
        {
            return null;
        }

        return await ReadContentAsync(pageId, cancellationToken);
    }

    public async Task<SaveDraftResult> SaveDraftAsync(Guid pageId, PageDocument document, int baseRevision, CancellationToken cancellationToken = default)
    {
        if (await pageRepository.GetByIdAsync(pageId, cancellationToken) is null)
        {
            return new SaveDraftResult.PageNotFound();
        }

        var (json, hash) = PageDocumentSerializer.Serialize(document);
        var stored = await pageDocumentRepository.GetDocumentsAsync(pageId, cancellationToken);
        var draft = stored.FirstOrDefault(d => d.Kind == PageDocumentDaoKind.Draft);
        var published = stored.FirstOrDefault(d => d.Kind == PageDocumentDaoKind.Published);

        // An autosave retry, or a save after undoing back to the saved state: nothing to write.
        if (draft is not null && draft.Revision == baseRevision && draft.ContentHash == hash)
        {
            return new SaveDraftResult.Saved(draft.Revision, draft.UpdatedAt, HasUnpublishedChanges(hash, published));
        }

        var updatedAt = timeProvider.GetUtcNow().UtcDateTime;
        var revision = await pageDocumentRepository.SaveDraftAsync(new PageDocumentDao
        {
            PageId = pageId,
            Kind = PageDocumentDaoKind.Draft,
            SchemaVersion = document.Version,
            Content = json,
            ContentHash = hash,
            UpdatedAt = updatedAt,
        }, baseRevision, cancellationToken);

        if (revision is null)
        {
            return new SaveDraftResult.Conflict(await ReadContentAsync(pageId, cancellationToken));
        }

        return new SaveDraftResult.Saved(revision.Value, updatedAt, HasUnpublishedChanges(hash, published));
    }

    private async Task<PageContent> ReadContentAsync(Guid pageId, CancellationToken cancellationToken)
    {
        var stored = await pageDocumentRepository.GetDocumentsAsync(pageId, cancellationToken);
        var draft = stored.FirstOrDefault(d => d.Kind == PageDocumentDaoKind.Draft);
        var published = stored.FirstOrDefault(d => d.Kind == PageDocumentDaoKind.Published);

        return new PageContent(
            pageId,
            draft is null ? PageDocument.Empty : PageDocumentSerializer.Deserialize(draft.Content, draft.SchemaVersion),
            draft?.Revision ?? 0,
            draft?.UpdatedAt,
            published is null ? null : PageDocumentSerializer.Deserialize(published.Content, published.SchemaVersion),
            published?.UpdatedAt,
            HasUnpublishedChanges(draft?.ContentHash ?? PageDocumentSerializer.Serialize(PageDocument.Empty).Hash, published));
    }

    private static bool HasUnpublishedChanges(string draftHash, PageDocumentDao? published) =>
        published is null || published.ContentHash != draftHash;
}
