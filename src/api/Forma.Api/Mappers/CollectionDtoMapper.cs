using Forma.Api.Dtos;
using Forma.Application.Entities;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers;

[Mapper]
public static partial class CollectionDtoMapper
{
    /// <summary>The collection, with its draft and the id of its detail page.</summary>
    public static CollectionDto ToDto(this Collection collection, Guid? detailPageId) => new(
        collection.Id,
        collection.WebsiteId,
        collection.Name,
        collection.SingularName,
        collection.Slug,
        collection.Icon,
        collection.Fields.Select(ToDto).ToList(),
        collection.DraftFields is { } fields
            ? new CollectionDraftDto(
                collection.DraftSlug ?? collection.Slug,
                fields.Select(ToDto).ToList(),
                collection.DraftValueMoves ?? new Dictionary<string, string?>(),
                collection.DraftUpdatedAt)
            : null,
        detailPageId,
        collection.CreatedAt,
        collection.UpdatedAt,
        collection.PublishedAt);

    public static partial CollectionFieldDto ToDto(this CollectionField field);

    /// <summary>The field as it is stored; a new field gets its id here.</summary>
    public static CollectionField ToField(this CollectionFieldRequest request) =>
        new(request.Id ?? $"fld_{Guid.NewGuid():N}"[..12], request.Label.Trim(), request.Type, request.Required);

    public static CollectionItemDto ToDto(this CollectionItem item) => new(
        item.Id,
        item.CollectionId,
        item.Slug,
        item.Values,
        item.DraftValues is { } values ? new CollectionItemDraftDto(item.DraftSlug ?? item.Slug, values, item.DraftUpdatedAt) : null,
        item.CreatedAt,
        item.UpdatedAt,
        item.PublishedAt);

    public static IReadOnlyList<CollectionItemDto> ToDtos(this IReadOnlyList<CollectionItem> items) => items.Select(ToDto).ToList();
}
