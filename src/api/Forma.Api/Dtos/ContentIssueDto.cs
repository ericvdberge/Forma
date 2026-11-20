using Forma.Application.Documents.Checks;

namespace Forma.Api.Dtos;

/// <summary>A problem in a page's content that a person can fix.</summary>
/// <param name="Severity"><c>error</c> blocks publishing; <c>warning</c> doesn't.</param>
/// <param name="Code">Stable code, e.g. <c>image.missing</c> or <c>link.placeholder</c>.</param>
/// <param name="Message">What is wrong, in plain language.</param>
/// <param name="SectionId">The section it is in; null when it is about the whole page.</param>
/// <param name="BlockId">The block it is about; null when it is about a section or the page.</param>
/// <param name="Field">The block prop or section setting, e.g. <c>href</c> or <c>background.image</c>.</param>
/// <param name="ItemIndex">The item of a list prop (a gallery image, a menu link), when it is about one.</param>
public record ContentIssueDto(
    IssueSeverity Severity,
    string Code,
    string Message,
    string? SectionId,
    string? BlockId,
    string? Field,
    int? ItemIndex);

/// <summary>The content issues of a page's draft.</summary>
/// <param name="DraftRevision">The draft revision that was checked.</param>
/// <param name="Issues">Errors first, then warnings, each in page order. Empty when there is nothing to fix.</param>
public record ContentCheckDto(int DraftRevision, IReadOnlyList<ContentIssueDto> Issues);

/// <summary>Why a page wasn't published.</summary>
/// <param name="Issues">All issues of the draft, errors first; at least one is an error.</param>
public record PublishRejectedDto(IReadOnlyList<ContentIssueDto> Issues);

/// <summary>Publishes a page's draft.</summary>
/// <param name="Revision">The draft revision the person saw when they chose Publish.</param>
public record PublishPageRequest(int Revision);
