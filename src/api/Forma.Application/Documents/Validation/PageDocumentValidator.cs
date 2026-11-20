using System.Text.Json;
using FluentValidation;

namespace Forma.Application.Documents.Validation;

/// <summary>
/// Level 1 (structural) rules for a page document, run on every draft save. A failure here is a bug in the
/// studio, not something a person can cause, so the save is rejected. The rules: <c>docs/design/page-content.md</c> § 6.
/// </summary>
public class PageDocumentValidator : AbstractValidator<PageDocument>
{
    public const int MaxDocumentBytes = 512 * 1024;

    public PageDocumentValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(d => d.Version)
            .Equal(PageDocument.CurrentVersion)
            .WithMessage($"Only version {PageDocument.CurrentVersion} documents can be saved.");

        RuleFor(d => d.Sections)
            .Must(s => s.Count <= DocumentRules.MaxSections)
            .WithMessage($"A page can have at most {DocumentRules.MaxSections} sections.");

        RuleForEach(d => d.Sections).SetValidator(new SectionValidator(sanitizer));

        RuleFor(d => d).Custom((document, context) =>
        {
            // Ids are how the studio selects and moves things, so they must be unique within the page.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var s = 0; s < document.Sections.Count; s++)
            {
                var section = document.Sections[s];
                if (!seen.Add(section.Id))
                {
                    context.AddFailure($"sections[{s}].id", $"The id '{section.Id}' is used more than once.");
                }

                for (var b = 0; b < section.Blocks.Count; b++)
                {
                    if (!seen.Add(section.Blocks[b].Id))
                    {
                        context.AddFailure($"sections[{s}].blocks[{b}].id", $"The id '{section.Blocks[b].Id}' is used more than once.");
                    }
                }
            }

            var size = JsonSerializer.SerializeToUtf8Bytes(document, PageDocumentJson.Options).Length;
            if (size > MaxDocumentBytes)
            {
                context.AddFailure("sections", $"The page is too large ({size / 1024} KB; at most {MaxDocumentBytes / 1024} KB).");
            }
        });
    }
}

public class SectionValidator : AbstractValidator<Section>
{
    public SectionValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(s => s.Id).Must(DocumentRules.IsId).WithMessage("Not a valid section id.");

        RuleFor(s => s.Settings).SetValidator(new SectionSettingsValidator());

        RuleFor(s => s.Blocks)
            .Must(b => b.Count <= DocumentRules.MaxBlocksPerSection)
            .WithMessage($"A section can have at most {DocumentRules.MaxBlocksPerSection} blocks.");

        RuleForEach(s => s.Blocks).ChildRules(block =>
        {
            block.RuleFor(b => b.Id).Must(DocumentRules.IsId).WithMessage("Not a valid block id.");
            block.RuleFor(b => b.Style).SetValidator(new BlockStyleValidator());
        });

        RuleForEach(s => s.Blocks).SetInheritanceValidator(v => v
            .Add(new HeadingBlockValidator(sanitizer))
            .Add(new TextBlockValidator(sanitizer))
            .Add(new ButtonBlockValidator())
            .Add(new BadgeBlockValidator())
            .Add(new ImageBlockValidator())
            .Add(new GalleryBlockValidator())
            .Add(new VideoBlockValidator())
            .Add(new DividerBlockValidator())
            .Add(new SpacerBlockValidator())
            .Add(new FormBlockValidator())
            .Add(new MapBlockValidator())
            .Add(new CarouselBlockValidator())
            .Add(new FaqBlockValidator())
            .Add(new TestimonialsBlockValidator())
            .Add(new FeaturesBlockValidator())
            .Add(new NavBlockValidator())
            .Add(new PluginBlockValidator()));
    }
}

public class SectionSettingsValidator : AbstractValidator<SectionSettings>
{
    public SectionSettingsValidator()
    {
        RuleFor(s => s.Name).PlainText(100);
        RuleFor(s => s.SplitRatio).InclusiveBetween(20, 80);
        // Past the last block just means everything is in the first column (e.g. a new, empty section).
        RuleFor(s => s.SplitAt).InclusiveBetween(0, DocumentRules.MaxBlocksPerSection);
        RuleFor(s => s.Padding).SetValidator(new ResponsiveSpacingValidator());
        RuleFor(s => s.Background).SetValidator(new BackgroundValidator());
        RuleFor(s => s.MinHeight).InclusiveBetween(0, 900);
        RuleFor(s => s.Gap).InclusiveBetween(0, 64);
        RuleFor(s => s.Radius).InclusiveBetween(0, 40);
        RuleFor(s => s.Animation.Delay).InclusiveBetween(0, 2);
        RuleFor(s => s.Anchor).Anchor();
        RuleFor(s => s.ClassName).ClassName();
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
        RuleFor(p => p.Tablet).SetValidator(new SpacingOverrideValidator()!).When(p => p.Tablet is not null);
        RuleFor(p => p.Mobile).SetValidator(new SpacingOverrideValidator()!).When(p => p.Mobile is not null);
    }

    private sealed class SpacingOverrideValidator : AbstractValidator<SpacingOverride>
    {
        public SpacingOverrideValidator()
        {
            RuleFor(p => p.Top).InclusiveBetween(0, Max).When(p => p.Top is not null);
            RuleFor(p => p.Bottom).InclusiveBetween(0, Max).When(p => p.Bottom is not null);
            RuleFor(p => p.Left).InclusiveBetween(0, Max).When(p => p.Left is not null);
            RuleFor(p => p.Right).InclusiveBetween(0, Max).When(p => p.Right is not null);
        }
    }
}

public class BackgroundValidator : AbstractValidator<Background>
{
    public BackgroundValidator()
    {
        RuleFor(b => b.Color).Color();
        RuleFor(b => b.Image).ImageSource();
        RuleFor(b => b.Position).Must(DocumentRules.IsPosition).WithMessage("Not a valid image position.");
        RuleFor(b => b.GradientFrom).Color();
        RuleFor(b => b.GradientTo).Color();
        RuleFor(b => b.GradientAngle).InclusiveBetween(0, 360);
        RuleFor(b => b.OverlayColor).Color();
        RuleFor(b => b.OverlayOpacity).InclusiveBetween(0, 100);
    }
}

public class BlockStyleValidator : AbstractValidator<BlockStyle>
{
    public BlockStyleValidator()
    {
        RuleFor(s => s.MarginTop).InclusiveBetween(0, 160);
        RuleFor(s => s.MarginBottom).InclusiveBetween(0, 160);
        RuleFor(s => s.Anchor).Anchor();
        RuleFor(s => s.ClassName).ClassName();
    }
}
