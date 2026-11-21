using Forma.Application.Entities;
using Forma.Application.Mappers;
using Forma.Infrastructure.Repositories.ScheduledPublishEntries;

namespace Forma.Application.Services;

/// <summary>Creates, reads and deletes what scheduled publishes put live. Rules and workflows belong to the callers.</summary>
public interface IScheduledPublishEntryCoreService
{
    Task<IReadOnlyList<ScheduledPublishEntry>> GetScheduledPublishEntriesAsync(ScheduledPublishEntryFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledPublishEntry>> CreateAsync(IReadOnlyList<ScheduledPublishEntryInsert> inserts, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entry. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ScheduledPublishEntryCoreService(IScheduledPublishEntryRepository repository) : IScheduledPublishEntryCoreService
{
    public async Task<IReadOnlyList<ScheduledPublishEntry>> GetScheduledPublishEntriesAsync(ScheduledPublishEntryFilter filter, CancellationToken cancellationToken = default)
    {
        var entries = await repository.GetScheduledPublishEntriesAsync(filter, cancellationToken);
        return entries.ToEntities();
    }

    public async Task<IReadOnlyList<ScheduledPublishEntry>> CreateAsync(IReadOnlyList<ScheduledPublishEntryInsert> inserts, CancellationToken cancellationToken = default)
    {
        var entries = inserts.Select(ToEntry).ToList();

        if (entries.Count > 0)
            await repository.AddAsync(entries.Select(e => e.ToDao()).ToList(), cancellationToken);

        return entries;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.DeleteAsync(new ScheduledPublishEntryFilter { Id = id }, cancellationToken) > 0;

    private static ScheduledPublishEntry ToEntry(ScheduledPublishEntryInsert insert) => new()
    {
        Id = Guid.CreateVersion7(),
        ScheduledPublishId = insert.ScheduledPublishId,
        CollectionId = insert.CollectionId,
        PageId = insert.PageId,
        CollectionItemId = insert.CollectionItemId,
    };
}
