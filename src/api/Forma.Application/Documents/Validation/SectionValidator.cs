using FluentValidation;

namespace Forma.Application.Documents.Validation;

public class SectionValidator : AbstractValidator<Section>
{
    public SectionValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(s => s.Id)
            .Must(DocumentRules.IsId).WithMessage("Not a valid section id.");

        RuleFor(s => s.Settings).SetValidator(new SectionSettingsValidator());

        RuleFor(s => s.Blocks.Count)
            .LessThanOrEqualTo(DocumentRules.MaxBlocksPerSection).WithMessage($"A section can have at most {DocumentRules.MaxBlocksPerSection} blocks.");

        RuleForEach(s => s.Blocks).SetValidator(new BlockValidator());

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
            .Add(new NavBlockValidator()));
    }
}
