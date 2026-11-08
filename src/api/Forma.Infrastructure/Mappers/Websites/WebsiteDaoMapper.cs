using Forma.Application.Entities.Websites;
using Forma.Infrastructure.DataAccessObjects.Websites;
using Riok.Mapperly.Abstractions;

namespace Forma.Infrastructure.Mappers.Websites;

[Mapper]
public static partial class WebsiteDaoMapper
{
    public static partial Website ToEntity(this WebsiteDao dao);

    public static partial WebsiteDao ToDao(this Website website);
}
