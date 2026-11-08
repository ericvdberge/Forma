using Forma.Application.Entities.Pages;
using Forma.Infrastructure.DataAccessObjects.Pages;
using Riok.Mapperly.Abstractions;

namespace Forma.Infrastructure.Mappers.Pages;

[Mapper]
public static partial class PageDaoMapper
{
    [MapperIgnoreSource(nameof(PageDao.Website))]
    public static partial Page ToEntity(this PageDao dao);

    [MapperIgnoreTarget(nameof(PageDao.Website))]
    public static partial PageDao ToDao(this Page page);
}
