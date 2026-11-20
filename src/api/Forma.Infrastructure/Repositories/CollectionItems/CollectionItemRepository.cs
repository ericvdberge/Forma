using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.CollectionItems;

public class CollectionItemRepository(FormaDbContext db) : ICollectionItemRepository
{
    public async Task<IReadOnlyList<CollectionItemDao>> GetCollectionItemsAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default) =>
        await db.CollectionItems
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CollectionItemDao item, CancellationToken cancellationToken = default)
    {
        db.CollectionItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task UpdateAsync(CollectionItemDao item, CancellationToken cancellationToken = default)
    {
        db.CollectionItems.Update(item);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.CollectionItems.Where(i => i.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
