using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.ScheduledPublishes;

/// <summary>
/// Criteria for querying scheduled publishes. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class ScheduledPublishFilter
{
    /// <summary>Only the publish with this id.</summary>
    public Guid? Id { get; init; }

    /// <summary>Only publishes of this website.</summary>
    public Guid? WebsiteId { get; init; }

    /// <summary>Only publishes with one of these statuses.</summary>
    public IReadOnlyCollection<ScheduledPublishDaoStatus>? Statuses { get; init; }

    /// <summary>Only publishes due at or before this time (UTC).</summary>
    public DateTime? DueBy { get; init; }
}
