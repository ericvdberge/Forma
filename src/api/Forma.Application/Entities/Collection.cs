namespace Forma.Application.Entities;

/// <summary>A website's own content type, such as <i>Blog posts</i>, with the fields its items fill in.</summary>
/// <remarks>See <c>docs/design/collections.md</c>.</remarks>
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

    /// <summary>The fields every item has, in display order.</summary>
    public required IReadOnlyList<CollectionField> Fields { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>One field of a collection.</summary>
/// <param name="Id">Stable id, e.g. <c>fld_ab12cd34</c>. Items and page bindings refer to it, so renaming a field changes nothing.</param>
/// <param name="Label">Shown in the item editor and the studio, e.g. <c>Cover image</c>.</param>
/// <param name="Type">What kind of value the field holds.</param>
/// <param name="Required">Whether every item must fill it in.</param>
public sealed record CollectionField(string Id, string Label, CollectionFieldType Type, bool Required);

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
