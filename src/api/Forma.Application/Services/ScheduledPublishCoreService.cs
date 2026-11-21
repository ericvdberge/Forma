using Forma.Application.Entities;
using Forma.Application.Mappers;
using Forma.Infrastructure.Repositories.ScheduledPublishes;

namespace Forma.Application.Services;

/// <summary>Creates, reads, updates and deletes scheduled publishes. Rules and workflows belong to the callers.</summary>
public interface IScheduledPublishCoreService
{
    Task<IReadOnlyList<ScheduledPublish>> GetScheduledPublishesAsync(ScheduledPublishFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The first publish matching <paramref name="filter"/>, or <c>null</c> when there is none.</summary>
    Task<ScheduledPublish?> GetScheduledPublishAsync(ScheduledPublishFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Creates a pending publish.</summary>
    Task<ScheduledPublish> CreateAsync(ScheduledPublishInsert insert, CancellationToken cancellationToken = default);

    /// <summary>Applies <paramref name="update"/>, or returns <c>null</c> when the publish does not exist.</summary>
    Task<ScheduledPublish?> UpdateAsync(Guid id, ScheduledPublishUpdate update, CancellationToken cancellationToken = default);

    /// <summary>Deletes the publish and its entries. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ScheduledPublishCoreService(IScheduledPublishRepository repository, TimeProvider timeProvider) : IScheduledPublishCoreService
{
    public async Task<IReadOnlyList<ScheduledPublish>> GetScheduledPublishesAsync(ScheduledPublishFilter filter, CancellationToken cancellationToken = default)
    {
        var publishes = await repository.GetScheduledPublishesAsync(filter, cancellationToken);
        return publishes.ToEntities();
    }

    public async Task<ScheduledPublish?> GetScheduledPublishAsync(ScheduledPublishFilter filter, CancellationToken cancellationToken = default)
    {
        var publishes = await repository.GetScheduledPublishesAsync(filter, cancellationToken);
        return publishes.FirstOrDefault()?.ToEntity();
    }

    public async Task<ScheduledPublish> CreateAsync(ScheduledPublishInsert insert, CancellationToken cancellationToken = default)
    {
        var publish = new ScheduledPublish
        {
            Id = Guid.CreateVersion7(),
            WebsiteId = insert.WebsiteId,
            PublishAt = insert.PublishAt,
            Status = ScheduledPublishStatus.Pending,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        await repository.AddAsync(publish.ToDao(), cancellationToken);

        return publish;
    }

    public async Task<ScheduledPublish?> UpdateAsync(Guid id, ScheduledPublishUpdate update, CancellationToken cancellationToken = default)
    {
        var publish = await GetScheduledPublishAsync(new ScheduledPublishFilter { Id = id }, cancellationToken);
        if (publish is null)
            return null;

        publish.PublishAt = update.PublishAt ?? publish.PublishAt;
        publish.Status = update.Status ?? publish.Status;
        publish.Issues = update.Issues ?? publish.Issues;
        publish.CompletedAt = update.CompletedAt ?? publish.CompletedAt;

        await repository.UpdateAsync(publish.ToDao(), cancellationToken);

        return publish;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.DeleteAsync(id, cancellationToken);
}
