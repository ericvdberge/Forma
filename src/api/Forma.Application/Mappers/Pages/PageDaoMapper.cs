using Forma.Application.Entities.Pages;
using Forma.Infrastructure.DataAccessObjects.Pages;
using Riok.Mapperly.Abstractions;

namespace Forma.Application.Mappers.Pages;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName)]
public static partial class PageDaoMapper
{
    [MapperIgnoreSource(nameof(PageDao.Website))]
    public static partial Page ToEntity(this PageDao dao);

    public static partial IReadOnlyList<Page> ToEntities(this IReadOnlyList<PageDao> daos);

    [MapperIgnoreTarget(nameof(PageDao.Website))]
    public static partial PageDao ToDao(this Page page);
}
