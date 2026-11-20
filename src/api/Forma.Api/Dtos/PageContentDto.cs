using Forma.Application.Documents;
using Forma.Application.Entities;

namespace Forma.Api.Dtos;

/// <summary>A page's Design Studio document.</summary>
/// <param name="PageId">The page the content belongs to.</param>
/// <param name="Document">The page's sections and blocks.</param>
/// <param name="Status"><c>published</c> once visitors see it; <c>draft</c> for sample content that was never published.</param>
/// <param name="CreatedAt">When the content was first stored (UTC).</param>
/// <param name="UpdatedAt">When the content last changed (UTC).</param>
public record PageContentDto(
    Guid PageId,
    PageDocument Document,
    PageContentStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Publishes a page: the document replaces what visitors see.</summary>
/// <param name="Document">The complete document, as the studio normalised it.</param>
public record PublishPageContentRequest(PageDocument Document);

/// <summary>The result of publishing a page.</summary>
/// <param name="Content">The page's content now.</param>
/// <param name="Warnings">Things worth a look that didn't block publishing, in page order. Empty when there are none.</param>
public record PublishedPageContentDto(PageContentDto Content, IReadOnlyList<ContentIssueDto> Warnings);
