using System.Text.Json;

namespace Forma.Application.Entities;

/// <summary>One item of a collection, e.g. one blog post: the live version visitors see and the draft being edited.</summary>
public class CollectionItem
{
    public Guid Id { get; set; }
    public Guid CollectionId { get; set; }

    /// <summary>The last part of the item's live address: <c>/{collection}/{slug}</c>.</summary>
    public required string Slug { get; set; }

    /// <summary>The item's live values, keyed by field id. A field without a value is left out.</summary>
    public required IReadOnlyDictionary<string, JsonElement> Values { get; set; }

    /// <summary>The draft's slug, or <c>null</c> when there is no draft.</summary>
    public string? DraftSlug { get; set; }

    /// <summary>The draft's values, or <c>null</c> when there is no draft.</summary>
    public IReadOnlyDictionary<string, JsonElement>? DraftValues { get; set; }

    public DateTime? DraftUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>When the item was last published, or <c>null</c> while it never was.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Whether visitors see the item.</summary>
    public bool IsLive => PublishedAt is not null;

    /// <summary>Whether there are changes to publish.</summary>
    public bool HasDraft => DraftValues is not null;
}
