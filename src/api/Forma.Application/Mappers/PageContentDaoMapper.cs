using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;

namespace Forma.Application.Mappers;

/// <summary>Maps page content to and from its stored form, where the live document and the draft are JSON.</summary>
public static class PageContentDaoMapper
{
    public static PageContent ToEntity(this PageContentDao dao) => new()
    {
        PageId = dao.PageId,
        Document = dao.Content is null ? null : PageDocumentSerializer.Deserialize(dao.Content, dao.SchemaVersion),
        DraftDocument = dao.DraftContent is null ? null : PageDocumentSerializer.Deserialize(dao.DraftContent, dao.DraftSchemaVersion ?? PageDocument.CurrentVersion),
        DraftUpdatedAt = dao.DraftUpdatedAt,
        PublishedAt = dao.PublishedAt,
        CreatedAt = dao.CreatedAt,
        UpdatedAt = dao.UpdatedAt,
    };

    public static IReadOnlyList<PageContent> ToEntities(this IReadOnlyList<PageContentDao> daos) =>
        daos.Select(ToEntity).ToList();

    public static PageContentDao ToDao(this PageContent content) => new()
    {
        PageId = content.PageId,
        SchemaVersion = content.Document?.Version ?? PageDocument.CurrentVersion,
        Content = content.Document is null ? null : PageDocumentSerializer.Serialize(content.Document),
        DraftSchemaVersion = content.DraftDocument?.Version,
        DraftContent = content.DraftDocument is null ? null : PageDocumentSerializer.Serialize(content.DraftDocument),
        DraftUpdatedAt = content.DraftUpdatedAt,
        PublishedAt = content.PublishedAt,
        CreatedAt = content.CreatedAt,
        UpdatedAt = content.UpdatedAt,
    };
}
