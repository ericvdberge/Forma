using Forma.Api.Dtos.Media;
using Forma.Application.Entities.Media;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers.Media;

[Mapper]
public static partial class MediaItemDtoMapper
{
    [MapperIgnoreSource(nameof(MediaItem.ImagePath))]
    [MapperIgnoreSource(nameof(MediaItem.ThumbnailPath))]
    [MapProperty(nameof(MediaItem.Id), nameof(MediaItemDto.ImageUrl), Use = nameof(ToImageUrl))]
    [MapProperty(nameof(MediaItem.Id), nameof(MediaItemDto.ThumbnailUrl), Use = nameof(ToThumbnailUrl))]
    public static partial MediaItemDto ToDto(this MediaItem media);

    public static partial IReadOnlyList<MediaItemDto> ToDtos(this IReadOnlyList<MediaItem> media);

    [UserMapping(Default = false)]
    private static string ToImageUrl(Guid id) => $"/media/{id}/image";

    [UserMapping(Default = false)]
    private static string ToThumbnailUrl(Guid id) => $"/media/{id}/thumbnail";
}
