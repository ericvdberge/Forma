using Forma.Api.Dtos.Websites;
using Forma.Application.Entities.Websites;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers.Websites;

[Mapper]
public static partial class WebsiteDtoMapper
{
    public static partial WebsiteDto ToDto(this Website website);

    public static partial IReadOnlyList<WebsiteDto> ToDtos(this IReadOnlyList<Website> websites);
}
