namespace Forma.Application.Documents.Validation;

public enum IssueSeverity
{
    /// <summary>Blocks publishing.</summary>
    Error,

    /// <summary>Worth a look, but doesn't block publishing.</summary>
    Warning,
}

/// <summary>
/// A problem a person can fix in a page's content, pointing at the section, block and field it is about
/// so the studio can select it.
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
    int? ItemIndex = null)
{
    /// <summary>Where the issue is, e.g. <c>s_hero.b_cta.href</c>; <c>document</c> when it is about the whole page.</summary>
    public string Location =>
        string.Join('.', new[] { SectionId, BlockId, ItemIndex is { } i ? $"{Field}[{i}]" : Field }.OfType<string>()) is { Length: > 0 } path
            ? path
            : "document";
}
