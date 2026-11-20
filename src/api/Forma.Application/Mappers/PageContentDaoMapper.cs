using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;

namespace Forma.Application.Mappers;

/// <summary>Maps page content to and from its stored form, where the document is JSON.</summary>
public static class PageContentDaoMapper
{
    public static PageContent ToEntity(this PageContentDao dao) => new()
    {
        PageId = dao.PageId,
        Document = PageDocumentSerializer.Deserialize(dao.Content, dao.SchemaVersion),
        Status = dao.Status == PageContentDaoStatus.Published ? PageContentStatus.Published : PageContentStatus.Draft,
        CreatedAt = dao.CreatedAt,
        UpdatedAt = dao.UpdatedAt,
    };

    public static PageContentDao ToDao(this PageContent content) => new()
    {
        PageId = content.PageId,
        Status = content.Status == PageContentStatus.Published ? PageContentDaoStatus.Published : PageContentDaoStatus.Draft,
        SchemaVersion = content.Document.Version,
        Content = PageDocumentSerializer.Serialize(content.Document),
        CreatedAt = content.CreatedAt,
        UpdatedAt = content.UpdatedAt,
    };
}
