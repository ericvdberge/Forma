using Forma.Application.Entities;

namespace Forma.Api.Dtos;

/// <summary>A website's collection, such as <i>Blog posts</i>: its live slug and fields, and its draft.</summary>
/// <param name="Id">The collection's id.</param>
/// <param name="WebsiteId">The website the collection belongs to.</param>
/// <param name="Name">Plural name, e.g. <c>Blog posts</c>.</param>
/// <param name="SingularName">What one item is called, e.g. <c>Blog post</c>.</param>
/// <param name="Slug">The first part of a live item's address: <c>/{slug}/{item}</c>.</param>
/// <param name="Icon">The icon the app shows it with, e.g. <c>newspaper</c>.</param>
/// <param name="Fields">The fields every live item has, in display order.</param>
/// <param name="Draft">The slug and fields that aren't live yet, or <c>null</c> when there are no changes.</param>
/// <param name="DetailPageId">The page every item is shown with, designed in the studio.</param>
/// <param name="CreatedAt">When the collection was created (UTC).</param>
/// <param name="UpdatedAt">When the collection last changed (UTC).</param>
/// <param name="PublishedAt">When the collection was last published (UTC), or <c>null</c> while it never was.</param>
public record CollectionDto(
    Guid Id,
    Guid WebsiteId,
    string Name,
    string SingularName,
    string Slug,
    string Icon,
    IReadOnlyList<CollectionFieldDto> Fields,
    CollectionDraftDto? Draft,
    Guid? DetailPageId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt);

/// <summary>A collection's draft.</summary>
/// <param name="Slug">The draft's slug.</param>
/// <param name="Fields">The draft's fields, in display order.</param>
/// <param name="ValueMoves">Where the values of removed fields go when it is published: removed field id → field id, or <c>null</c> to remove them.</param>
/// <param name="UpdatedAt">When the draft was last saved (UTC).</param>
public record CollectionDraftDto(
    string Slug,
    IReadOnlyList<CollectionFieldDto> Fields,
    IReadOnlyDictionary<string, string?> ValueMoves,
    DateTime? UpdatedAt);

/// <summary>One field of a collection.</summary>
/// <param name="Id">Stable id, e.g. <c>fld_ab12cd34</c>; item values and page bindings refer to it.</param>
/// <param name="Label">Shown in the item editor and the studio, e.g. <c>Cover image</c>.</param>
/// <param name="Type">What kind of value the field holds.</param>
/// <param name="Required">Whether every item must fill it in.</param>
public record CollectionFieldDto(string Id, string Label, CollectionFieldType Type, bool Required);

/// <summary>Creates a collection, and the detail page its items are shown with. It is a draft until it is published.</summary>
/// <param name="WebsiteId">The website the collection belongs to.</param>
/// <param name="Name">Plural name, e.g. <c>Blog posts</c>.</param>
/// <param name="SingularName">What one item is called, e.g. <c>Blog post</c>.</param>
/// <param name="Slug">Lowercase words separated by dashes, e.g. <c>blog</c>; unique within the website.</param>
/// <param name="Icon">The icon the app shows it with, e.g. <c>newspaper</c>.</param>
/// <param name="Fields">The fields, in display order.</param>
public record CreateCollectionRequest(
    Guid WebsiteId,
    string Name,
    string SingularName,
    string Slug,
    string Icon,
    IReadOnlyList<CollectionFieldRequest> Fields);

/// <summary>Changes what can't break anything and is saved directly: a collection's names and icon.</summary>
/// <param name="Name">Plural name, e.g. <c>Blog posts</c>.</param>
/// <param name="SingularName">What one item is called, e.g. <c>Blog post</c>.</param>
/// <param name="Icon">The icon the app shows it with, e.g. <c>newspaper</c>.</param>
public record UpdateCollectionRequest(string Name, string SingularName, string Icon);

/// <summary>Saves a collection's draft: its slug and fields, and where the values of removed fields go.</summary>
/// <param name="Slug">Lowercase words separated by dashes, e.g. <c>blog</c>; unique within the website.</param>
/// <param name="Fields">The complete list of fields, in display order.</param>
/// <param name="ValueMoves">For live fields the draft removes: the field their values move to, or <c>null</c> to remove them.</param>
public record SaveCollectionDraftRequest(
    string Slug,
    IReadOnlyList<CollectionFieldRequest> Fields,
    IReadOnlyDictionary<string, string?> ValueMoves);

/// <summary>A field as the collection editor sends it.</summary>
/// <param name="Id">The field's id; <c>null</c> for a new field, which is given one. The editor may also choose one (<c>fld_…</c>).</param>
/// <param name="Label">Shown in the item editor and the studio, e.g. <c>Cover image</c>.</param>
/// <param name="Type">What kind of value the field holds.</param>
/// <param name="Required">Whether every item must fill it in.</param>
public record CollectionFieldRequest(string? Id, string Label, CollectionFieldType Type, bool Required);

/// <summary>A page whose live content shows a field of the collection.</summary>
/// <param name="FieldId">The field.</param>
/// <param name="PageId">The page.</param>
/// <param name="PageTitle">The page's title.</param>
/// <param name="Kind"><c>binding</c>: a block on the detail page is connected to it; <c>cards</c>: Collection cards show it.</param>
public record FieldUsageDto(string FieldId, Guid PageId, string PageTitle, PageBindingUsageKind Kind);
