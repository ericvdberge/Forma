namespace Forma.Application.Documents.Checks;

public enum IssueSeverity
{
    /// <summary>Blocks publishing.</summary>
    Error,

    /// <summary>Shown before publishing, but doesn't block it.</summary>
    Warning,
}

/// <summary>
/// A problem a person can fix in a page's content (level 2 of <c>docs/design/page-content.md</c> § 6), pointing at
/// the section, block and field it is about so the studio can select it.
/// </summary>
/// <param name="Severity">Whether it blocks publishing.</param>
/// <param name="Code">Stable machine-readable code, e.g. <c>image.missing</c>.</param>
/// <param name="Message">What is wrong, in plain language.</param>
/// <param name="SectionId">The section it is in, when it is about part of the page.</param>
/// <param name="BlockId">The block it is about, when it is about a block.</param>
/// <param name="Field">The block prop or section setting, e.g. <c>href</c> or <c>background.image</c>.</param>
/// <param name="ItemIndex">The item of a list prop (a gallery image, a nav link), when it is about one.</param>
public sealed record ContentIssue(
    IssueSeverity Severity,
    string Code,
    string Message,
    string? SectionId = null,
    string? BlockId = null,
    string? Field = null,
    int? ItemIndex = null);

/// <summary>What the checks need to know beyond the document itself.</summary>
/// <param name="WebsiteId">The website the page belongs to: media and links must be its own.</param>
/// <param name="PageId">The page being checked.</param>
public sealed record CheckContext(Guid WebsiteId, Guid PageId);

/// <summary>Finds content issues (level 2): the rules a page must pass before it is published.</summary>
public interface IPageDocumentChecker
{
    Task<IReadOnlyList<ContentIssue>> CheckAsync(PageDocument document, CheckContext context, CancellationToken cancellationToken = default);
}
