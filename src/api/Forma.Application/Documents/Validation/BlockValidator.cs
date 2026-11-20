using FluentValidation;

namespace Forma.Application.Documents.Validation;

/// <summary>The rules every block has in common; the props rules per block type are in <c>BlockValidators.cs</c>.</summary>
public class BlockValidator : AbstractValidator<Block>
{
    public BlockValidator()
    {
        RuleFor(b => b.Id)
            .Must(DocumentRules.IsId).WithMessage("Not a valid block id.");

        RuleFor(b => b.Style).SetValidator(new BlockStyleValidator());

        RuleForEach(b => b.Bindings)
            .Must((block, binding) => BindableProps.IsBindable(block, binding.Key))
            .WithMessage((_, binding) => $"‘{binding.Key}’ can't be connected to a collection field.")
            .Must(binding => DocumentRules.IsFieldId(binding.Value.Field))
            .WithMessage("Not a valid field id.")
            .OverridePropertyName("bindings");
    }
}

public class BlockStyleValidator : AbstractValidator<BlockStyle>
{
    public BlockStyleValidator()
    {
        RuleFor(s => s.MarginTop).InclusiveBetween(0, 160);

        RuleFor(s => s.MarginBottom).InclusiveBetween(0, 160);

        RuleFor(s => s.Anchor)
            .Must(DocumentRules.IsAnchor).WithMessage("Anchors may only contain a–z, 0–9 and dashes (at most 60).");

        RuleFor(s => s.ClassName)
            .MaximumLength(DocumentRules.MaxClassNameLength)
            .Must(DocumentRules.IsClassName).WithMessage("Class names may only contain letters, digits, _ and -, separated by spaces.");
    }
}
