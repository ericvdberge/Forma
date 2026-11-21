using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.ScheduledPublishes;

public interface IScheduledPublishRepository
{
    Task<IReadOnlyList<ScheduledPublishDao>> GetScheduledPublishesAsync(ScheduledPublishFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(ScheduledPublishDao publish, CancellationToken cancellationToken = default);

    Task UpdateAsync(ScheduledPublishDao publish, CancellationToken cancellationToken = default);

    /// <summary>Deletes the publish and its entries. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ScheduledPublishRepository(FormaDbContext db) : IScheduledPublishRepository
{
    public async Task<IReadOnlyList<ScheduledPublishDao>> GetScheduledPublishesAsync(ScheduledPublishFilter filter, CancellationToken cancellationToken = default) =>
        await db.ScheduledPublishes
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderBy(p => p.PublishAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ScheduledPublishDao publish, CancellationToken cancellationToken = default)
    {
        db.ScheduledPublishes.Add(publish);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task UpdateAsync(ScheduledPublishDao publish, CancellationToken cancellationToken = default)
    {
        db.ScheduledPublishes.Update(publish);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.ScheduledPublishes.ApplyFilter(new ScheduledPublishFilter { Id = id }).ExecuteDeleteAsync(cancellationToken) > 0;
}
