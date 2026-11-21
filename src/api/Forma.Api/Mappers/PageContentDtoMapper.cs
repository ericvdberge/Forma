using Forma.Api.Dtos;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Riok.Mapperly.Abstractions;

namespace Forma.Api.Mappers;

/// <summary>Documents are passed through as is: the document model is the API contract.</summary>
[Mapper]
public static partial class PageContentDtoMapper
{
    /// <summary>
    /// The content in <paramref name="version"/>: the draft (or the live document without one), or the live document.
    /// <c>null</c> when there is no document in that version.
    /// </summary>
    public static PageContentDto? ToDto(this PageContent content, ContentVersion version)
    {
        var (document, shown) = version == ContentVersion.Draft && content.DraftDocument is { } draft
            ? (draft, ContentVersion.Draft)
            : (content.Document, ContentVersion.Live);

        if (document is null)
            return null;

        return new PageContentDto(
            content.PageId,
            document,
            shown,
            content.HasDraft,
            content.DraftUpdatedAt,
            content.PublishedAt,
            content.CreatedAt,
            content.UpdatedAt);
    }

    [MapperIgnoreSource(nameof(ContentIssue.Location))]
    public static partial ContentIssueDto ToDto(this ContentIssue issue);

    public static partial IReadOnlyList<ContentIssueDto> ToDtos(this IReadOnlyList<ContentIssue> issues);
}
