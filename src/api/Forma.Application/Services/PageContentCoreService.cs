using Forma.Application.Documents;
using Forma.Application.Documents.Checks;
using Forma.Application.Entities;
using Forma.Application.Plugins;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Repositories.PageDocuments;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Services;

/// <summary>Reads and saves the Design Studio's page documents.</summary>
/// <remarks>See <c>docs/design/page-content.md</c>.</remarks>
public interface IPageContentCoreService
{
    /// <summary>The page's draft and published document, or null when the page does not exist.</summary>
    Task<PageContent?> GetContentAsync(Guid pageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the page's draft with <paramref name="document"/> when <paramref name="baseRevision"/> is the
    /// current draft revision. The document has passed structural validation in the API before this is called.
    /// </summary>
    Task<SaveDraftResult> SaveDraftAsync(Guid pageId, PageDocument document, int baseRevision, CancellationToken cancellationToken = default);

    /// <summary>The content issues of the page's current draft, or null when the page does not exist.</summary>
    Task<ContentCheck?> CheckAsync(Guid pageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the page's draft when it is still at <paramref name="draftRevision"/> and has no error-level issues.
    /// </summary>
    Task<PublishResult> PublishAsync(Guid pageId, int draftRevision, CancellationToken cancellationToken = default);
}

public class PageContentCoreService(
    IPageRepository pageRepository,
    IPageDocumentRepository pageDocumentRepository,
    IPageDocumentChecker checker,
    IPluginEventPublisher pluginEvents,
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
        if (await pageRepository.GetByIdAsync(pageId, cancellationToken) is not { } page)
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

        pluginEvents.Publish(PluginEvents.PageDraftSaved, new PageDraftSavedData(PageInfo(page), revision.Value, document));
        return new SaveDraftResult.Saved(revision.Value, updatedAt, HasUnpublishedChanges(hash, published));
    }

    public async Task<ContentCheck?> CheckAsync(Guid pageId, CancellationToken cancellationToken = default)
    {
        if (await pageRepository.GetByIdAsync(pageId, cancellationToken) is not { } page)
        {
            return null;
        }

        var content = await ReadContentAsync(pageId, cancellationToken);
        var issues = await checker.CheckAsync(content.Draft, new CheckContext(page.WebsiteId, pageId), cancellationToken);
        return new ContentCheck(content.DraftRevision, issues);
    }

    public async Task<PublishResult> PublishAsync(Guid pageId, int draftRevision, CancellationToken cancellationToken = default)
    {
        if (await pageRepository.GetByIdAsync(pageId, cancellationToken) is not { } page)
        {
            return new PublishResult.PageNotFound();
        }

        var content = await ReadContentAsync(pageId, cancellationToken);
        if (content.DraftRevision != draftRevision)
        {
            return new PublishResult.Conflict(content);
        }

        // Nothing saved yet: there is no draft to copy.
        if (draftRevision == 0)
        {
            return new PublishResult.Rejected([new(IssueSeverity.Error, "page.not_saved", "The page has no saved content to publish yet.")]);
        }

        var issues = await checker.CheckAsync(content.Draft, new CheckContext(page.WebsiteId, pageId), cancellationToken);
        if (issues.Any(i => i.Severity == IssueSeverity.Error))
        {
            return new PublishResult.Rejected(issues);
        }

        var published = await pageDocumentRepository.PublishDraftAsync(pageId, draftRevision, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        var current = await ReadContentAsync(pageId, cancellationToken);
        if (!published)
        {
            return new PublishResult.Conflict(current);
        }

        pluginEvents.Publish(PluginEvents.PagePublished, new PagePublishedData(PageInfo(page), draftRevision, content.Draft));
        return new PublishResult.Published(current);
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

    private static PluginPageInfo PageInfo(PageDao page) => new(page.Id, page.WebsiteId, page.Title, page.Slug);

    private static bool HasUnpublishedChanges(string draftHash, PageDocumentDao? published) =>
        published is null || published.ContentHash != draftHash;
}
