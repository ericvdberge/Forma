using System.Text.Json;
using Forma.Application.Entities;

namespace Forma.Api.Dtos;

/// <summary>One item of a collection, e.g. one blog post.</summary>
/// <param name="Id">The item's id.</param>
/// <param name="CollectionId">The collection the item belongs to.</param>
/// <param name="Slug">The last part of the item's address: <c>/{collection}/{slug}</c>.</param>
/// <param name="Status"><c>published</c> once visitors see it.</param>
/// <param name="Values">The item's values keyed by field id: a string, number or boolean depending on the field's type.</param>
/// <param name="CreatedAt">When the item was created (UTC).</param>
/// <param name="UpdatedAt">When the item last changed (UTC).</param>
/// <param name="PublishedAt">When the item was last published (UTC), or <c>null</c> when it never was.</param>
public record CollectionItemDto(
    Guid Id,
    Guid CollectionId,
    string Slug,
    CollectionItemStatus Status,
    IReadOnlyDictionary<string, JsonElement> Values,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt);

/// <summary>Creates or saves an item.</summary>
/// <param name="Slug">Lowercase words separated by dashes, e.g. <c>opening-our-studio</c>; unique within the collection.</param>
/// <param name="Values">The item's values keyed by field id. Leave a field out (or empty) when it has no value.</param>
/// <param name="Status"><c>published</c> to show the item to visitors, <c>draft</c> to keep it hidden.</param>
public record SaveCollectionItemRequest(
    string Slug,
    IReadOnlyDictionary<string, JsonElement> Values,
    CollectionItemStatus Status);
