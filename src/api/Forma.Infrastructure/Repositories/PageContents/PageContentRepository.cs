using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.PageContents;

public class PageContentRepository(FormaDbContext db) : IPageContentRepository
{
    public async Task<IReadOnlyList<PageContentDao>> GetPageContentsAsync(PageContentFilter filter, CancellationToken cancellationToken = default) =>
        await db.PageContents
            .AsNoTracking()
            .ApplyFilter(filter)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PageContentDao content, CancellationToken cancellationToken = default)
    {
        db.PageContents.Add(content);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task UpdateAsync(PageContentDao content, CancellationToken cancellationToken = default)
    {
        db.PageContents.Update(content);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(Guid pageId, CancellationToken cancellationToken = default) =>
        await db.PageContents.Where(c => c.PageId == pageId).ExecuteDeleteAsync(cancellationToken) > 0;
}
