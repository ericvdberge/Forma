using System.Text.Json;

namespace Forma.Application.Entities;

/// <summary>One item of a collection, e.g. one blog post.</summary>
public class CollectionItem
{
    public Guid Id { get; set; }
    public Guid CollectionId { get; set; }

    /// <summary>The last part of the item's address: <c>/{collection}/{slug}</c>.</summary>
    public required string Slug { get; set; }

    public CollectionItemStatus Status { get; set; } = CollectionItemStatus.Draft;

    /// <summary>The item's values, keyed by field id. A field without a value is left out.</summary>
    public required IReadOnlyDictionary<string, JsonElement> Values { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>When the item was last published, or <c>null</c> when it never was.</summary>
    public DateTime? PublishedAt { get; set; }
}

public enum CollectionItemStatus
{
    Draft,
    Published,
}
