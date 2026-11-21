namespace Forma.Infrastructure.Repositories.CollectionItems;

/// <summary>
/// Criteria for querying collection items. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class CollectionItemFilter
{
    /// <summary>Only the item with this id.</summary>
    public Guid? Id { get; init; }

    /// <summary>Only items of this collection.</summary>
    public Guid? CollectionId { get; init; }

    /// <summary>Only items of collections that belong to this website.</summary>
    public Guid? WebsiteId { get; init; }

    /// <summary>Only the item with exactly this slug.</summary>
    public string? Slug { get; init; }

    /// <summary><c>true</c>: only items visitors see (published at least once); <c>false</c>: only items never published.</summary>
    public bool? IsLive { get; init; }

    /// <summary><c>true</c>: only items with a draft; <c>false</c>: only items without one.</summary>
    public bool? HasDraft { get; init; }
}
