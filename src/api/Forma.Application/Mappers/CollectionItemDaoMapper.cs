using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;

namespace Forma.Application.Mappers;

/// <summary>Maps collection items to and from their stored form, where the values (live and draft) are JSON objects.</summary>
public static class CollectionItemDaoMapper
{
    public static CollectionItem ToEntity(this CollectionItemDao dao) => new()
    {
        Id = dao.Id,
        CollectionId = dao.CollectionId,
        Slug = dao.Slug,
        Values = ReadValues(dao.Values) ?? [],
        DraftSlug = dao.DraftSlug,
        DraftValues = ReadValues(dao.DraftValues),
        DraftUpdatedAt = dao.DraftUpdatedAt,
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
        Values = JsonSerializer.Serialize(item.Values, PageDocumentJson.Options),
        DraftSlug = item.DraftSlug,
        DraftValues = item.DraftValues is null ? null : JsonSerializer.Serialize(item.DraftValues, PageDocumentJson.Options),
        DraftUpdatedAt = item.DraftUpdatedAt,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        PublishedAt = item.PublishedAt,
    };

    private static Dictionary<string, JsonElement>? ReadValues(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, PageDocumentJson.Options);
}
