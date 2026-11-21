using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.ScheduledPublishEntries;

/// <summary>
/// Criteria for querying what scheduled publishes put live. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class ScheduledPublishEntryFilter
{
    /// <summary>Only the entry with this id.</summary>
    public Guid? Id { get; init; }

    /// <summary>Only entries of this publish.</summary>
    public Guid? ScheduledPublishId { get; init; }

    /// <summary>Only entries of publishes of this website.</summary>
    public Guid? WebsiteId { get; init; }

    /// <summary>Only entries of publishes with one of these statuses.</summary>
    public IReadOnlyCollection<ScheduledPublishDaoStatus>? PublishStatuses { get; init; }

    /// <summary>Only the entry of this collection.</summary>
    public Guid? CollectionId { get; init; }

    /// <summary>Only the entry of this page.</summary>
    public Guid? PageId { get; init; }

    /// <summary>Only the entry of this item.</summary>
    public Guid? CollectionItemId { get; init; }
}
