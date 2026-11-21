using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Pages;

public class PageRepository(FormaDbContext db) : IPageRepository
{
    public async Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default) =>
        await db.Pages
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PageDao page, CancellationToken cancellationToken = default)
    {
        db.Pages.Add(page);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task UpdateAsync(PageDao page, CancellationToken cancellationToken = default)
    {
        db.Pages.Update(page);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Pages.ApplyFilter(new PageFilter { Id = id }).ExecuteDeleteAsync(cancellationToken) > 0;
}
