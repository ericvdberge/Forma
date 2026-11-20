using System.Text.Json;
using FluentValidation;

namespace Forma.Application.Documents.Validation;

/// <summary>
/// The structural rules for a page document: a failure here is a bug in the studio, not something a person can cause.
/// The rules: <c>docs/design/page-content.md</c> § 6.
/// </summary>
public class PageDocumentValidator : AbstractValidator<PageDocument>
{
    public PageDocumentValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(d => d.Version)
            .Equal(PageDocument.CurrentVersion).WithMessage($"Only version {PageDocument.CurrentVersion} documents can be saved.");

        RuleFor(d => d.Sections.Count)
            .LessThanOrEqualTo(DocumentRules.MaxSections).WithMessage($"A page can have at most {DocumentRules.MaxSections} sections.");

        RuleForEach(d => d.Sections).SetValidator(new SectionValidator(sanitizer));

        RuleFor(d => d)
            .Must(HaveUniqueIds).WithMessage("Every section and block needs its own id.");

        RuleFor(d => d)
            .Must(FitTheSizeLimit).WithMessage($"The page is too large (at most {DocumentRules.MaxDocumentBytes / 1024} KB).");
    }

    // Ids are how the studio selects and moves things, so they must be unique within the page.
    private static bool HaveUniqueIds(PageDocument document)
    {
        var ids = document.Sections.SelectMany(s => s.Blocks.Select(b => b.Id).Prepend(s.Id)).ToList();

        return ids.Distinct(StringComparer.Ordinal).Count() == ids.Count;
    }

    private static bool FitTheSizeLimit(PageDocument document) =>
        JsonSerializer.SerializeToUtf8Bytes(document, PageDocumentJson.Options).Length <= DocumentRules.MaxDocumentBytes;
}
