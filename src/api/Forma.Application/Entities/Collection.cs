namespace Forma.Application.Entities;

/// <summary>
/// A website's own content type, such as <i>Blog posts</i>, with the fields its items fill in. Its slug and fields have a
/// live version and a draft; its names and icon are saved directly.
/// </summary>
/// <remarks>See <c>docs/design/collections.md</c> and <c>docs/design/drafts-and-publish.md</c>.</remarks>
public class Collection
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }

    /// <summary>Plural name, e.g. <c>Blog posts</c>.</summary>
    public required string Name { get; set; }

    /// <summary>What one item is called, e.g. <c>Blog post</c>.</summary>
    public required string SingularName { get; set; }

    /// <summary>The first part of an item's address: <c>/{slug}/{item}</c>.</summary>
    public required string Slug { get; set; }

    /// <summary>The icon the app shows it with, one of <see cref="CollectionIcons.All"/>.</summary>
    public required string Icon { get; set; }

    /// <summary>The fields every live item has, in display order.</summary>
    public required IReadOnlyList<CollectionField> Fields { get; set; }

    /// <summary>The draft's slug, or <c>null</c> when there is no draft.</summary>
    public string? DraftSlug { get; set; }

    /// <summary>The draft's fields, or <c>null</c> when there is no draft.</summary>
    public IReadOnlyList<CollectionField>? DraftFields { get; set; }

    /// <summary>Where the values of the draft's removed fields go: removed field id → field id, or <c>null</c> to drop them.</summary>
    public IReadOnlyDictionary<string, string?>? DraftValueMoves { get; set; }

    public DateTime? DraftUpdatedAt { get; set; }

    /// <summary>When the collection was last published, or <c>null</c> while it never was.</summary>
    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Whether visitors see the collection.</summary>
    public bool IsLive => PublishedAt is not null;

    /// <summary>Whether there are changes to publish.</summary>
    public bool HasDraft => DraftFields is not null;
}

/// <summary>One field of a collection.</summary>
/// <param name="Id">Stable id, e.g. <c>fld_ab12cd34</c>. Items and page bindings refer to it, so renaming a field changes nothing.</param>
/// <param name="Label">Shown in the item editor and the studio, e.g. <c>Cover image</c>.</param>
/// <param name="Type">What kind of value the field holds.</param>
/// <param name="Required">Whether every item must fill it in.</param>
public sealed record CollectionField(string Id, string Label, CollectionFieldType Type, bool Required);

/// <summary>
/// Icons a collection can be shown with in the app's sidebar. Mirrors <c>collectionIcons</c> in
/// <c>src/app/src/utils/collections.ts</c>.
/// </summary>
public static class CollectionIcons
{
    public const string Default = "database";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
    [
        "database", "newspaper", "users", "briefcase", "calendar", "shopping-bag", "star",
        "book-open", "image", "map-pin", "utensils", "music", "graduation-cap", "heart",
    ]);
}

public enum CollectionFieldType
{
    /// <summary>A line of plain text (at most 500 characters).</summary>
    Text,

    /// <summary>Formatted text, with the studio's rich-text markup.</summary>
    RichText,

    /// <summary>An image from the media library: <c>/media/{id}/image</c>.</summary>
    Image,

    /// <summary>A link: <c>https://</c>, <c>/page</c>, <c>mailto:</c>…</summary>
    Url,

    Number,

    /// <summary>An ISO date, <c>yyyy-MM-dd</c>.</summary>
    Date,

    Boolean,
}
