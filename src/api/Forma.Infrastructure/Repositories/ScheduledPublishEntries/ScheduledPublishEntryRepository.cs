using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.ScheduledPublishEntries;

public interface IScheduledPublishEntryRepository
{
    Task<IReadOnlyList<ScheduledPublishEntryDao>> GetScheduledPublishEntriesAsync(ScheduledPublishEntryFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(IReadOnlyList<ScheduledPublishEntryDao> entries, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entries matching <paramref name="filter"/>, and returns how many there were.</summary>
    Task<int> DeleteAsync(ScheduledPublishEntryFilter filter, CancellationToken cancellationToken = default);
}

public class ScheduledPublishEntryRepository(FormaDbContext db) : IScheduledPublishEntryRepository
{
    public async Task<IReadOnlyList<ScheduledPublishEntryDao>> GetScheduledPublishEntriesAsync(ScheduledPublishEntryFilter filter, CancellationToken cancellationToken = default) =>
        await db.ScheduledPublishEntries
            .AsNoTracking()
            .ApplyFilter(filter)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IReadOnlyList<ScheduledPublishEntryDao> entries, CancellationToken cancellationToken = default)
    {
        db.ScheduledPublishEntries.AddRange(entries);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<int> DeleteAsync(ScheduledPublishEntryFilter filter, CancellationToken cancellationToken = default) =>
        await db.ScheduledPublishEntries.ApplyFilter(filter).ExecuteDeleteAsync(cancellationToken);
}
