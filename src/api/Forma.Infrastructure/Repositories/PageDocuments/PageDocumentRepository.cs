using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;

namespace Forma.Infrastructure.Repositories.PageDocuments;

// Skeleton for review: implemented together with the PageDocuments table and its migration.
public class PageDocumentRepository(FormaDbContext db) : IPageDocumentRepository
{
    private readonly FormaDbContext _db = db;

    public Task<IReadOnlyList<PageDocumentDao>> GetDocumentsAsync(Guid pageId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <remarks>
    /// Plan: expectedRevision 0 → INSERT (a unique-key violation means someone else inserted first → null);
    /// otherwise ExecuteUpdate … WHERE PageId = @id AND Kind = 'Draft' AND Revision = @expected (0 rows → null).
    /// </remarks>
    public Task<int?> SaveDraftAsync(PageDocumentDao draft, int expectedRevision, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
