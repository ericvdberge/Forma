namespace Forma.Application.Entities;

/// <summary>The values a new collection is created with.</summary>
public class CollectionInsert
{
    public Guid WebsiteId { get; init; }
    public required string Name { get; init; }
    public required string SingularName { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyList<CollectionField> Fields { get; init; }
}
