using Forma.Api.Dtos;
using Forma.Application.Entities;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers;

[Mapper]
public static partial class PageDtoMapper
{
    public static partial PageDto ToDto(this Page page);

    public static partial IReadOnlyList<PageDto> ToDtos(this IReadOnlyList<Page> pages);
}
