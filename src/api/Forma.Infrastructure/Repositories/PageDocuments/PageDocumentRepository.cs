using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.PageDocuments;

public class PageDocumentRepository(FormaDbContext db) : IPageDocumentRepository
{
    public async Task<IReadOnlyList<PageDocumentDao>> GetDocumentsAsync(Guid pageId, CancellationToken cancellationToken = default) =>
        await db.PageDocuments
            .AsNoTracking()
            .Where(d => d.PageId == pageId)
            .ToListAsync(cancellationToken);

    public async Task<int?> SaveDraftAsync(PageDocumentDao draft, int expectedRevision, CancellationToken cancellationToken = default)
    {
        var newRevision = expectedRevision + 1;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (expectedRevision == 0)
        {
            // First save: the primary key (PageId, Kind) makes a concurrent first save fail instead of overwriting.
            db.PageDocuments.Add(new PageDocumentDao
            {
                PageId = draft.PageId,
                Kind = PageDocumentDaoKind.Draft,
                SchemaVersion = draft.SchemaVersion,
                Content = draft.Content,
                ContentHash = draft.ContentHash,
                Revision = newRevision,
                UpdatedAt = draft.UpdatedAt,
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                return null;
            }
        }
        else
        {
            // One conditional statement: only the save that still sees `expectedRevision` wins.
            var updated = await db.PageDocuments
                .Where(d => d.PageId == draft.PageId && d.Kind == PageDocumentDaoKind.Draft && d.Revision == expectedRevision)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.SchemaVersion, draft.SchemaVersion)
                    .SetProperty(d => d.Content, draft.Content)
                    .SetProperty(d => d.ContentHash, draft.ContentHash)
                    .SetProperty(d => d.Revision, newRevision)
                    .SetProperty(d => d.UpdatedAt, draft.UpdatedAt),
                    cancellationToken);
            if (updated == 0)
            {
                return null;
            }
        }

        // The Pages list sorts by UpdatedAt, so an edited page moves to the top.
        await db.Pages
            .Where(p => p.Id == draft.PageId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UpdatedAt, draft.UpdatedAt), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return newRevision;
    }

    public async Task AddAsync(IEnumerable<PageDocumentDao> documents, CancellationToken cancellationToken = default)
    {
        db.PageDocuments.AddRange(documents);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }
}
