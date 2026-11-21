using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;

namespace Forma.Application.Mappers;

/// <summary>Maps collections to and from their stored form, where the fields are JSON.</summary>
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
        Fields = JsonSerializer.Deserialize<List<CollectionField>>(dao.Fields, PageDocumentJson.Options) ?? [],
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
        CreatedAt = collection.CreatedAt,
        UpdatedAt = collection.UpdatedAt,
    };
}
