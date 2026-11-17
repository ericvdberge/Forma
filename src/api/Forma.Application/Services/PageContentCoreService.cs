using Forma.Application.Documents;
using Forma.Application.Entities;
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

// Skeleton for review: the shape of the class is final, the bodies come with the implementation.
public class PageContentCoreService(
    IPageRepository pageRepository,
    IPageDocumentRepository pageDocumentRepository) : IPageContentCoreService
{
    private readonly IPageRepository _pageRepository = pageRepository;
    private readonly IPageDocumentRepository _pageDocumentRepository = pageDocumentRepository;

    /// <remarks>
    /// Plan: page exists? → load both document rows → deserialise (upgrading older schema versions) →
    /// draft defaults to <see cref="PageDocument.Empty"/>; HasUnpublishedChanges compares the content hashes.
    /// </remarks>
    public Task<PageContent?> GetContentAsync(Guid pageId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <remarks>
    /// Plan: page exists? → serialise canonically with <see cref="PageDocumentJson.Options"/> and hash →
    /// same hash as the current draft: return Saved with the current revision →
    /// otherwise <see cref="IPageDocumentRepository.SaveDraftAsync"/> with the base revision (null = Conflict) →
    /// touch Pages.UpdatedAt.
    /// </remarks>
    public Task<SaveDraftResult> SaveDraftAsync(Guid pageId, PageDocument document, int baseRevision, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
