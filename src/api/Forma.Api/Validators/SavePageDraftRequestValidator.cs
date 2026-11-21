using FluentValidation;
using Forma.Api.Dtos;
using Forma.Application.Documents;

namespace Forma.Api.Validators;

/// <summary>
/// The document's structural rules (<see cref="Forma.Application.Documents.Validation.PageDocumentValidator"/>).
/// Its JSON shape (block types, enums, unknown or missing properties) is enforced while the body is read.
/// </summary>
public class SavePageDraftRequestValidator : AbstractValidator<SavePageDraftRequest>
{
    public SavePageDraftRequestValidator(IValidator<PageDocument> documentValidator)
    {
        RuleFor(r => r.Document).SetValidator(documentValidator);
    }
}
