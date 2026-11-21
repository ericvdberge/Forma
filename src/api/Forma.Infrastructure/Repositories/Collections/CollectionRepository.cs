using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Collections;

public class CollectionRepository(FormaDbContext db) : ICollectionRepository
{
    public async Task<IReadOnlyList<CollectionDao>> GetCollectionsAsync(CollectionFilter filter, CancellationToken cancellationToken = default) =>
        await db.Collections
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CollectionDao collection, CancellationToken cancellationToken = default)
    {
        db.Collections.Add(collection);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task UpdateAsync(CollectionDao collection, CancellationToken cancellationToken = default)
    {
        db.Collections.Update(collection);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Collections.ApplyFilter(new CollectionFilter { Id = id }).ExecuteDeleteAsync(cancellationToken) > 0;
}
