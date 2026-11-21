using Forma.Api.Dtos;
using Forma.Application.Entities;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers;

[Mapper]
public static partial class CollectionDtoMapper
{
    /// <summary>The collection with the id of its detail page.</summary>
    public static CollectionDto ToDto(this Collection collection, Guid? detailPageId) => new(
        collection.Id,
        collection.WebsiteId,
        collection.Name,
        collection.SingularName,
        collection.Slug,
        collection.Icon,
        collection.Fields.Select(ToDto).ToList(),
        detailPageId,
        collection.CreatedAt,
        collection.UpdatedAt);

    public static partial CollectionFieldDto ToDto(this CollectionField field);

    /// <summary>The field as it is stored; a new field gets its id here.</summary>
    public static CollectionField ToField(this CollectionFieldRequest request) =>
        new(request.Id ?? $"fld_{Guid.NewGuid():N}"[..12], request.Label.Trim(), request.Type, request.Required);

    public static partial CollectionItemDto ToDto(this CollectionItem item);

    public static partial IReadOnlyList<CollectionItemDto> ToDtos(this IReadOnlyList<CollectionItem> items);
}
