using FluentValidation;

namespace Forma.Application.Documents.Validation;

public class SectionSettingsValidator : AbstractValidator<SectionSettings>
{
    public SectionSettingsValidator()
    {
        RuleFor(s => s.Name).MaximumLength(100);

        RuleFor(s => s.SplitRatio).InclusiveBetween(20, 80);

        // Past the last block just means everything is in the first column (e.g. a new, empty section).
        RuleFor(s => s.SplitAt).InclusiveBetween(0, DocumentRules.MaxBlocksPerSection);

        RuleFor(s => s.Padding).SetValidator(new ResponsiveSpacingValidator());

        RuleFor(s => s.Background).SetValidator(new BackgroundValidator());

        RuleFor(s => s.MinHeight).InclusiveBetween(0, 900);

        RuleFor(s => s.Gap).InclusiveBetween(0, 64);

        RuleFor(s => s.Radius).InclusiveBetween(0, 40);

        RuleFor(s => s.Animation.Delay).InclusiveBetween(0, 2);

        RuleFor(s => s.Anchor)
            .Must(DocumentRules.IsAnchor).WithMessage("Anchors may only contain a–z, 0–9 and dashes (at most 60).");

        RuleFor(s => s.ClassName)
            .MaximumLength(DocumentRules.MaxClassNameLength)
            .Must(DocumentRules.IsClassName).WithMessage("Class names may only contain letters, digits, _ and -, separated by spaces.");
    }
}

public class BackgroundValidator : AbstractValidator<Background>
{
    private const string ColorMessage = "Colours must be hex, e.g. #1a1a1a.";

    public BackgroundValidator()
    {
        RuleFor(b => b.Color)
            .Must(DocumentRules.IsColor).WithMessage(ColorMessage);

        RuleFor(b => b.Image)
            .Must(DocumentRules.IsImageSource).WithMessage("Background images must come from the media library.");

        RuleFor(b => b.Position)
            .Must(DocumentRules.IsPosition).WithMessage("Not a valid image position.");

        RuleFor(b => b.GradientFrom)
            .Must(DocumentRules.IsColor).WithMessage(ColorMessage);

        RuleFor(b => b.GradientTo)
            .Must(DocumentRules.IsColor).WithMessage(ColorMessage);

        RuleFor(b => b.GradientAngle).InclusiveBetween(0, 360);

        RuleFor(b => b.OverlayColor)
            .Must(DocumentRules.IsColor).WithMessage(ColorMessage);

        RuleFor(b => b.OverlayOpacity).InclusiveBetween(0, 100);
    }
}

public class ResponsiveSpacingValidator : AbstractValidator<ResponsiveSpacing>
{
    private const int Max = 400;

    public ResponsiveSpacingValidator()
    {
        RuleFor(p => p.Desktop.Top).InclusiveBetween(0, Max);

        RuleFor(p => p.Desktop.Bottom).InclusiveBetween(0, Max);

        RuleFor(p => p.Desktop.Left).InclusiveBetween(0, Max);

        RuleFor(p => p.Desktop.Right).InclusiveBetween(0, Max);

        RuleFor(p => p.Tablet!).SetValidator(new SpacingOverrideValidator()).When(p => p.Tablet is not null);

        RuleFor(p => p.Mobile!).SetValidator(new SpacingOverrideValidator()).When(p => p.Mobile is not null);
    }

    private sealed class SpacingOverrideValidator : AbstractValidator<SpacingOverride>
    {
        public SpacingOverrideValidator()
        {
            RuleFor(p => p.Top).InclusiveBetween(0, Max);

            RuleFor(p => p.Bottom).InclusiveBetween(0, Max);

            RuleFor(p => p.Left).InclusiveBetween(0, Max);

            RuleFor(p => p.Right).InclusiveBetween(0, Max);
        }
    }
}
