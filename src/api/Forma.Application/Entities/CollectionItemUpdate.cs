using System.Text.Json;

namespace Forma.Application.Entities;

/// <summary>
/// Changes to a collection item. Every property is optional: a property that is <c>null</c> is left as it is.
/// Publishing is an update that copies the draft to <see cref="Slug"/> and <see cref="Values"/>, clears the draft and sets <see cref="PublishedAt"/>.
/// </summary>
public class CollectionItemUpdate
{
    public string? Slug { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? Values { get; init; }

    /// <summary>The draft's slug; saving a draft also sets <see cref="CollectionItem.DraftUpdatedAt"/>.</summary>
    public string? DraftSlug { get; init; }

    public IReadOnlyDictionary<string, JsonElement>? DraftValues { get; init; }

    /// <summary><c>true</c> removes the draft (after publishing it or discarding it).</summary>
    public bool? ClearDraft { get; init; }

    public DateTime? PublishedAt { get; init; }
}
