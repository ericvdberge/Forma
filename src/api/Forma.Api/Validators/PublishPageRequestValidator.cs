using FluentValidation;
using Forma.Api.Dtos;

namespace Forma.Api.Validators;

public class PublishPageRequestValidator : AbstractValidator<PublishPageRequest>
{
    public PublishPageRequestValidator()
    {
        RuleFor(r => r.Revision).GreaterThanOrEqualTo(0);
    }
}
