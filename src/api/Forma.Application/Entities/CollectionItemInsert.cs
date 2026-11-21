using System.Text.Json;

namespace Forma.Application.Entities;

/// <summary>
/// The values a new collection item is created with. A new item has a draft and isn't live until it is published;
/// its live slug and values start out the same as the draft's.
/// </summary>
public class CollectionItemInsert
{
    public Guid CollectionId { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyDictionary<string, JsonElement> Values { get; init; }
    public string? DraftSlug { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? DraftValues { get; init; }
    public DateTime? PublishedAt { get; init; }
}
