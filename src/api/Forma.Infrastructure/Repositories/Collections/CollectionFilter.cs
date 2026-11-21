namespace Forma.Infrastructure.Repositories.Collections;

/// <summary>
/// Criteria for querying collections. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class CollectionFilter
{
    /// <summary>Only the collection with this id.</summary>
    public Guid? Id { get; init; }

    /// <summary>Only collections that belong to this website.</summary>
    public Guid? WebsiteId { get; init; }

    /// <summary>Only the collection with exactly this slug.</summary>
    public string? Slug { get; init; }
}
