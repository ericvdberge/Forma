using System.Text.Json;

namespace Forma.Api.Dtos;

/// <summary>One item of a collection, e.g. one blog post: its live version and its draft.</summary>
/// <param name="Id">The item's id.</param>
/// <param name="CollectionId">The collection the item belongs to.</param>
/// <param name="Slug">The last part of the item's live address: <c>/{collection}/{slug}</c>.</param>
/// <param name="Values">The item's live values keyed by field id: a string, number or boolean depending on the field's type.</param>
/// <param name="Draft">The changes that aren't live yet, or <c>null</c> when there are none.</param>
/// <param name="CreatedAt">When the item was created (UTC).</param>
/// <param name="UpdatedAt">When the item last changed (UTC).</param>
/// <param name="PublishedAt">When the item was last published (UTC), or <c>null</c> while it never was: visitors don't see it yet.</param>
public record CollectionItemDto(
    Guid Id,
    Guid CollectionId,
    string Slug,
    IReadOnlyDictionary<string, JsonElement> Values,
    CollectionItemDraftDto? Draft,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt);

/// <summary>An item's draft.</summary>
/// <param name="Slug">The draft's slug.</param>
/// <param name="Values">The draft's values keyed by field id.</param>
/// <param name="UpdatedAt">When the draft was last saved (UTC).</param>
public record CollectionItemDraftDto(string Slug, IReadOnlyDictionary<string, JsonElement> Values, DateTime? UpdatedAt);

/// <summary>Creates an item (as a draft) or saves an item's draft.</summary>
/// <param name="Slug">Lowercase words separated by dashes, e.g. <c>opening-our-studio</c>; unique within the collection.</param>
/// <param name="Values">The item's values keyed by field id. Leave a field out (or empty) when it has no value.</param>
public record SaveCollectionItemRequest(string Slug, IReadOnlyDictionary<string, JsonElement> Values);
