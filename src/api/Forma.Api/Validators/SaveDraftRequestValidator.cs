using FluentValidation;
using Forma.Api.Dtos;
using Forma.Application.Documents;

namespace Forma.Api.Validators;

/// <summary>
/// Checks a draft before it reaches the core service: the request itself, then the document's structural rules
/// (<see cref="Forma.Application.Documents.Validation.PageDocumentValidator"/>). The JSON shape (block types, enums,
/// unknown or missing properties) has already been enforced while reading the body; see <see cref="PageDocumentJson"/>.
/// </summary>
public class SaveDraftRequestValidator : AbstractValidator<SaveDraftRequest>
{
    public SaveDraftRequestValidator(IValidator<PageDocument> documentValidator)
    {
        RuleFor(r => r.BaseRevision).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Document).SetValidator(documentValidator);
    }
}
