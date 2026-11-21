namespace Forma.Application.Entities;

/// <summary>
/// The values a new collection is created with. A new collection has a draft and isn't live until it is published;
/// its live slug and fields start out the same as the draft's.
/// </summary>
public class CollectionInsert
{
    public Guid WebsiteId { get; init; }
    public required string Name { get; init; }
    public required string SingularName { get; init; }
    public required string Slug { get; init; }
    public string Icon { get; init; } = CollectionIcons.Default;
    public required IReadOnlyList<CollectionField> Fields { get; init; }
    public string? DraftSlug { get; init; }
    public IReadOnlyList<CollectionField>? DraftFields { get; init; }
    public DateTime? PublishedAt { get; init; }
}
