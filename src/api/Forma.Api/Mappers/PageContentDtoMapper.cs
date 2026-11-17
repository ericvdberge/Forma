using Forma.Api.Dtos;
using Forma.Application.Entities;
using Forma.Application.Services;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers;

/// <summary>Documents are passed through as is: the document model is the API contract.</summary>
[Mapper]
public static partial class PageContentDtoMapper
{
    public static partial PageContentDto ToDto(this PageContent content);

    public static partial SavedDraftDto ToDto(this SaveDraftResult.Saved saved);
}
