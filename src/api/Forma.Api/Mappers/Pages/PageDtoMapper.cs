using Forma.Api.Dtos.Pages;
using Forma.Application.Entities.Pages;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers.Pages;

[Mapper]
public static partial class PageDtoMapper
{
    public static partial PageDto ToDto(this Page page);

    public static partial IReadOnlyList<PageDto> ToDtos(this IReadOnlyList<Page> pages);
}
