namespace Forma.Application.Entities;

/// <summary>
/// Changes to a collection. Every property is optional: a property that is <c>null</c> is left as it is.
/// Publishing is an update that copies the draft to <see cref="Slug"/> and <see cref="Fields"/>, clears the draft and sets <see cref="PublishedAt"/>.
/// </summary>
public class CollectionUpdate
{
    public string? Name { get; init; }
    public string? SingularName { get; init; }
    public string? Slug { get; init; }
    public string? Icon { get; init; }
    public IReadOnlyList<CollectionField>? Fields { get; init; }

    /// <summary>The draft's slug; saving a draft also sets <see cref="Collection.DraftUpdatedAt"/>.</summary>
    public string? DraftSlug { get; init; }

    public IReadOnlyList<CollectionField>? DraftFields { get; init; }

    /// <summary>Where the values of the draft's removed fields go: removed field id → field id, or <c>null</c> to drop them.</summary>
    public IReadOnlyDictionary<string, string?>? DraftValueMoves { get; init; }

    /// <summary><c>true</c> removes the draft (after publishing it or discarding it).</summary>
    public bool? ClearDraft { get; init; }

    public DateTime? PublishedAt { get; init; }
}
