using FluentValidation;
using Forma.Api.Dtos;
using Forma.Application.Documents;

namespace Forma.Api.Validators;

/// <summary>
/// Checks a draft before it reaches the core service. The JSON shape (block types, enums, unknown or missing
/// properties) has already been enforced while reading the body; see <see cref="PageDocumentJson"/>.
/// </summary>
public class SaveDraftRequestValidator : AbstractValidator<SaveDraftRequest>
{
    public SaveDraftRequestValidator()
    {
        RuleFor(r => r.BaseRevision).GreaterThanOrEqualTo(0);

        RuleFor(r => r.Document.Version)
            .Equal(PageDocument.CurrentVersion)
            .WithMessage($"Only version {PageDocument.CurrentVersion} documents can be saved.")
            .OverridePropertyName("document.version");

        // TODO(phase 1, after review): RuleFor(r => r.Document).SetValidator(PageDocumentValidator), the level 1 rules
        // in docs/design/page-content.md § 6. It lives in Forma.Application (which then references FluentValidation),
        // so saved items and imports reuse it.
    }
}
