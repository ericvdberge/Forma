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

    /// <summary>Only the collection with exactly this (live) slug.</summary>
    public string? Slug { get; init; }

    /// <summary><c>true</c>: only collections visitors see (published at least once); <c>false</c>: only those never published.</summary>
    public bool? IsLive { get; init; }

    /// <summary><c>true</c>: only collections with a draft; <c>false</c>: only those without one.</summary>
    public bool? HasDraft { get; init; }
}
