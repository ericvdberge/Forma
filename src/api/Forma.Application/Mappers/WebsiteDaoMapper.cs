using Forma.Application.Entities;
using Forma.Infrastructure.Daos;
using Riok.Mapperly.Abstractions;

namespace Forma.Application.Mappers;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName)]
public static partial class WebsiteDaoMapper
{
    public static partial Website ToEntity(this WebsiteDao dao);

    public static partial IReadOnlyList<Website> ToEntities(this IReadOnlyList<WebsiteDao> daos);

    public static partial WebsiteDao ToDao(this Website website);
}
