using Forma.Api.Dtos;
using Forma.Application.Entities;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers;

[Mapper]
public static partial class WebsiteDtoMapper
{
    public static partial WebsiteDto ToDto(this Website website);

    public static partial IReadOnlyList<WebsiteDto> ToDtos(this IReadOnlyList<Website> websites);
}
