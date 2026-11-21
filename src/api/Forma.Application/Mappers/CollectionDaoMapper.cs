using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;

namespace Forma.Application.Mappers;

/// <summary>Maps collections to and from their stored form, where the fields (live and draft) are JSON.</summary>
public static class CollectionDaoMapper
{
    public static Collection ToEntity(this CollectionDao dao) => new()
    {
        Id = dao.Id,
        WebsiteId = dao.WebsiteId,
        Name = dao.Name,
        SingularName = dao.SingularName,
        Slug = dao.Slug,
        Icon = dao.Icon,
        Fields = ReadFields(dao.Fields) ?? [],
        DraftSlug = dao.DraftSlug,
        DraftFields = ReadFields(dao.DraftFields),
        DraftValueMoves = dao.DraftValueMoves is null ? null : JsonSerializer.Deserialize<Dictionary<string, string?>>(dao.DraftValueMoves, PageDocumentJson.Options),
        DraftUpdatedAt = dao.DraftUpdatedAt,
        PublishedAt = dao.PublishedAt,
        CreatedAt = dao.CreatedAt,
        UpdatedAt = dao.UpdatedAt,
    };

    public static IReadOnlyList<Collection> ToEntities(this IReadOnlyList<CollectionDao> daos) =>
        daos.Select(ToEntity).ToList();

    public static CollectionDao ToDao(this Collection collection) => new()
    {
        Id = collection.Id,
        WebsiteId = collection.WebsiteId,
        Name = collection.Name,
        SingularName = collection.SingularName,
        Slug = collection.Slug,
        Icon = collection.Icon,
        Fields = JsonSerializer.Serialize(collection.Fields, PageDocumentJson.Options),
        DraftSlug = collection.DraftSlug,
        DraftFields = collection.DraftFields is null ? null : JsonSerializer.Serialize(collection.DraftFields, PageDocumentJson.Options),
        DraftValueMoves = collection.DraftValueMoves is null ? null : JsonSerializer.Serialize(collection.DraftValueMoves, PageDocumentJson.Options),
        DraftUpdatedAt = collection.DraftUpdatedAt,
        PublishedAt = collection.PublishedAt,
        CreatedAt = collection.CreatedAt,
        UpdatedAt = collection.UpdatedAt,
    };

    private static List<CollectionField>? ReadFields(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<List<CollectionField>>(json, PageDocumentJson.Options);
}
