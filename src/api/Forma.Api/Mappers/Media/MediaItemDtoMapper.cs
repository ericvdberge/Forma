using Forma.Api.Assets;
using Forma.Api.Dtos.Media;
using Forma.Application.Entities.Media;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers.Media;

[Mapper]
public static partial class MediaItemDtoMapper
{
    [MapProperty(nameof(MediaItem.FilePath), nameof(MediaItemDto.Url), Use = nameof(ToAssetUrl))]
    [MapProperty(nameof(MediaItem.ThumbnailPath), nameof(MediaItemDto.ThumbnailUrl), Use = nameof(ToAssetUrl))]
    public static partial MediaItemDto ToDto(this MediaItem media);

    public static partial IReadOnlyList<MediaItemDto> ToDtos(this IReadOnlyList<MediaItem> media);

    [UserMapping(Default = false)]
    private static string ToAssetUrl(string path) => $"{AssetFiles.RequestPath}/{path}";
}
