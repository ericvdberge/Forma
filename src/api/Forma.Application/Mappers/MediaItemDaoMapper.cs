using Forma.Application.Entities;
using Forma.Infrastructure.Daos;
using Riok.Mapperly.Abstractions;

namespace Forma.Application.Mappers;

[Mapper]
public static partial class MediaItemDaoMapper
{
    [MapperIgnoreSource(nameof(MediaItemDao.Website))]
    public static partial MediaItem ToEntity(this MediaItemDao dao);

    public static partial IReadOnlyList<MediaItem> ToEntities(this IReadOnlyList<MediaItemDao> daos);

    [MapperIgnoreTarget(nameof(MediaItemDao.Website))]
    public static partial MediaItemDao ToDao(this MediaItem media);
}
