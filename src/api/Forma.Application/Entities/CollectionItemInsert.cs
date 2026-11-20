using System.Text.Json;

namespace Forma.Application.Entities;

/// <summary>The values a new collection item is created with.</summary>
public class CollectionItemInsert
{
    public Guid CollectionId { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyDictionary<string, JsonElement> Values { get; init; }
    public CollectionItemStatus Status { get; init; } = CollectionItemStatus.Draft;
}
