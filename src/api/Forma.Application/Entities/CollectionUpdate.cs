namespace Forma.Application.Entities;

/// <summary>Changes to a collection. Every property is optional: a property that is <c>null</c> is left as it is.</summary>
public class CollectionUpdate
{
    public string? Name { get; init; }
    public string? SingularName { get; init; }
    public string? Slug { get; init; }
    public IReadOnlyList<CollectionField>? Fields { get; init; }
}
