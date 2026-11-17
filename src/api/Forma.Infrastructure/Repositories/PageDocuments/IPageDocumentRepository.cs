using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.PageDocuments;

public interface IPageDocumentRepository
{
    /// <summary>The page's stored documents: none, a draft, or a draft and a published one.</summary>
    Task<IReadOnlyList<PageDocumentDao>> GetDocumentsAsync(Guid pageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes <paramref name="draft"/> as the page's draft with revision <paramref name="expectedRevision"/> + 1, but only
    /// while the stored draft still has <paramref name="expectedRevision"/> (0 = no draft stored yet). One conditional
    /// statement, so two concurrent saves can't both win. Sets the page's <c>UpdatedAt</c> in the same transaction,
    /// so the Pages list shows recently edited pages first.
    /// </summary>
    /// <returns>The new revision, or null when the stored revision was a different one.</returns>
    Task<int?> SaveDraftAsync(PageDocumentDao draft, int expectedRevision, CancellationToken cancellationToken = default);

    /// <summary>Adds documents for pages that have none yet, e.g. sample content.</summary>
    Task AddAsync(IEnumerable<PageDocumentDao> documents, CancellationToken cancellationToken = default);
}
