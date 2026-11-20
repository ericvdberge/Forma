using Forma.Application.Entities;

namespace Forma.Api.Dtos;

/// <summary>A website's collection, such as <i>Blog posts</i>, with its fields.</summary>
/// <param name="Id">The collection's id.</param>
/// <param name="WebsiteId">The website the collection belongs to.</param>
/// <param name="Name">Plural name, e.g. <c>Blog posts</c>.</param>
/// <param name="SingularName">What one item is called, e.g. <c>Blog post</c>.</param>
/// <param name="Slug">The first part of an item's address: <c>/{slug}/{item}</c>.</param>
/// <param name="Icon">The icon the app shows it with, e.g. <c>newspaper</c>.</param>
/// <param name="Fields">The fields every item has, in display order.</param>
/// <param name="DetailPageId">The page every item is shown with, designed in the studio.</param>
/// <param name="CreatedAt">When the collection was created (UTC).</param>
/// <param name="UpdatedAt">When the collection last changed (UTC).</param>
public record CollectionDto(
    Guid Id,
    Guid WebsiteId,
    string Name,
    string SingularName,
    string Slug,
    string Icon,
    IReadOnlyList<CollectionFieldDto> Fields,
    Guid? DetailPageId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>One field of a collection.</summary>
/// <param name="Id">Stable id, e.g. <c>fld_ab12cd34</c>; item values and page bindings refer to it.</param>
/// <param name="Label">Shown in the item editor and the studio, e.g. <c>Cover image</c>.</param>
/// <param name="Type">What kind of value the field holds.</param>
/// <param name="Required">Whether every item must fill it in.</param>
public record CollectionFieldDto(string Id, string Label, CollectionFieldType Type, bool Required);

/// <summary>Creates a collection, and the detail page its items are shown with.</summary>
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

/// <summary>Changes a collection: its names, slug and fields. Items keep their values for fields that stay.</summary>
/// <param name="Name">Plural name, e.g. <c>Blog posts</c>.</param>
/// <param name="SingularName">What one item is called, e.g. <c>Blog post</c>.</param>
/// <param name="Slug">Lowercase words separated by dashes, e.g. <c>blog</c>; unique within the website.</param>
/// <param name="Icon">The icon the app shows it with, e.g. <c>newspaper</c>.</param>
/// <param name="Fields">The complete list of fields, in display order.</param>
public record UpdateCollectionRequest(
    string Name,
    string SingularName,
    string Slug,
    string Icon,
    IReadOnlyList<CollectionFieldRequest> Fields);

/// <summary>A field as the collection editor sends it.</summary>
/// <param name="Id">The field's id; <c>null</c> for a new field, which is given one.</param>
/// <param name="Label">Shown in the item editor and the studio, e.g. <c>Cover image</c>.</param>
/// <param name="Type">What kind of value the field holds.</param>
/// <param name="Required">Whether every item must fill it in.</param>
public record CollectionFieldRequest(string? Id, string Label, CollectionFieldType Type, bool Required);
