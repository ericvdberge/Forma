using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;

namespace Forma.Application.Mappers;

/// <summary>Maps collection items to and from their stored form, where the values are a JSON object.</summary>
public static class CollectionItemDaoMapper
{
    public static CollectionItem ToEntity(this CollectionItemDao dao) => new()
    {
        Id = dao.Id,
        CollectionId = dao.CollectionId,
        Slug = dao.Slug,
        Status = dao.Status == CollectionItemDaoStatus.Published ? CollectionItemStatus.Published : CollectionItemStatus.Draft,
        Values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(dao.Values, PageDocumentJson.Options) ?? [],
        CreatedAt = dao.CreatedAt,
        UpdatedAt = dao.UpdatedAt,
        PublishedAt = dao.PublishedAt,
    };

    public static IReadOnlyList<CollectionItem> ToEntities(this IReadOnlyList<CollectionItemDao> daos) =>
        daos.Select(ToEntity).ToList();

    public static CollectionItemDao ToDao(this CollectionItem item) => new()
    {
        Id = item.Id,
        CollectionId = item.CollectionId,
        Slug = item.Slug,
        Status = item.Status == CollectionItemStatus.Published ? CollectionItemDaoStatus.Published : CollectionItemDaoStatus.Draft,
        Values = JsonSerializer.Serialize(item.Values, PageDocumentJson.Options),
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        PublishedAt = item.PublishedAt,
    };

    public static CollectionItemDaoStatus ToDao(this CollectionItemStatus status) =>
        status == CollectionItemStatus.Published ? CollectionItemDaoStatus.Published : CollectionItemDaoStatus.Draft;
}
