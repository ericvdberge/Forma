using Forma.Api.Dtos;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers;

/// <summary>Documents are passed through as is: the document model is the API contract.</summary>
[Mapper]
public static partial class PageContentDtoMapper
{
    public static partial PageContentDto ToDto(this PageContent content);

    [MapperIgnoreSource(nameof(ContentIssue.Location))]
    public static partial ContentIssueDto ToDto(this ContentIssue issue);

    public static partial IReadOnlyList<ContentIssueDto> ToDtos(this IReadOnlyList<ContentIssue> issues);
}
