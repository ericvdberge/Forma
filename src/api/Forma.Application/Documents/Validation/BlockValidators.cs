using FluentValidation;

namespace Forma.Application.Documents.Validation;

// Props rules per block type. Ranges mirror the inspector fields in src/app/src/studio/blocks.ts.

public class HeadingBlockValidator : AbstractValidator<HeadingBlock>
{
    public HeadingBlockValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(b => b.Props.Html)
            .MaximumLength(DocumentRules.MaxHtmlLength)
            .Must(sanitizer.IsClean).WithMessage("The heading contains markup that isn't allowed.");

        RuleFor(b => b.Props.Level).InclusiveBetween(1, 4);
    }
}

public class TextBlockValidator : AbstractValidator<TextBlock>
{
    public TextBlockValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(b => b.Props.Html)
            .MaximumLength(DocumentRules.MaxHtmlLength)
            .Must(sanitizer.IsClean).WithMessage("The text contains markup that isn't allowed.");
    }
}

public class ButtonBlockValidator : AbstractValidator<ButtonBlock>
{
    public ButtonBlockValidator()
    {
        RuleFor(b => b.Props.Label).MaximumLength(100);

        RuleFor(b => b.Props.Href)
            .MaximumLength(DocumentRules.MaxUrlLength)
            .Must(DocumentRules.IsLink).WithMessage("Button links must start with https://, http://, mailto:, tel:, / or #.");
    }
}

public class BadgeBlockValidator : AbstractValidator<BadgeBlock>
{
    public BadgeBlockValidator()
    {
        RuleFor(b => b.Props.Text).MaximumLength(100);
    }
}

public class ImageBlockValidator : AbstractValidator<ImageBlock>
{
    public ImageBlockValidator()
    {
        RuleFor(b => b.Props.Src)
            .Must(DocumentRules.IsImageSource).WithMessage("Images must come from the media library.");

        RuleFor(b => b.Props.Alt).MaximumLength(DocumentRules.MaxTextLength);

        RuleFor(b => b.Props.Caption).MaximumLength(DocumentRules.MaxTextLength);

        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class GalleryBlockValidator : AbstractValidator<GalleryBlock>
{
    public GalleryBlockValidator()
    {
        RuleFor(b => b.Props.Images.Count)
            .LessThanOrEqualTo(DocumentRules.MaxListItems).WithMessage($"A gallery can have at most {DocumentRules.MaxListItems} images.");

        RuleForEach(b => b.Props.Images).ChildRules(image =>
        {
            image.RuleFor(i => i.Src)
                .Must(DocumentRules.IsImageSource).WithMessage("Gallery images must come from the media library.");

            image.RuleFor(i => i.Alt).MaximumLength(DocumentRules.MaxTextLength);
        });

        RuleFor(b => b.Props.Columns).InclusiveBetween(1, 4);

        RuleFor(b => b.Props.Gap).InclusiveBetween(0, 40);

        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class VideoBlockValidator : AbstractValidator<VideoBlock>
{
    public VideoBlockValidator()
    {
        RuleFor(b => b.Props.Url)
            .MaximumLength(DocumentRules.MaxUrlLength)
            .Must(DocumentRules.IsVideoUrl).WithMessage("Videos must be a YouTube or Vimeo link.");

        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class DividerBlockValidator : AbstractValidator<DividerBlock>
{
    public DividerBlockValidator()
    {
        RuleFor(b => b.Props.Thickness).InclusiveBetween(1, 6);
    }
}

public class SpacerBlockValidator : AbstractValidator<SpacerBlock>
{
    public SpacerBlockValidator()
    {
        RuleFor(b => b.Props.Height).InclusiveBetween(8, 240);
    }
}

public class FormBlockValidator : AbstractValidator<FormBlock>
{
    public FormBlockValidator()
    {
        RuleFor(b => b.Props.Fields.Count)
            .LessThanOrEqualTo(DocumentRules.MaxListItems).WithMessage($"A form can have at most {DocumentRules.MaxListItems} fields.");

        RuleForEach(b => b.Props.Fields).ChildRules(field => field.RuleFor(f => f.Label).MaximumLength(100));

        RuleFor(b => b.Props.SubmitLabel).MaximumLength(100);

        RuleFor(b => b.Props.SuccessMessage).MaximumLength(DocumentRules.MaxTextLength);
    }
}

public class MapBlockValidator : AbstractValidator<MapBlock>
{
    public MapBlockValidator()
    {
        RuleFor(b => b.Props.Address).MaximumLength(200);

        RuleFor(b => b.Props.Zoom).InclusiveBetween(3, 19);

        RuleFor(b => b.Props.Height).InclusiveBetween(160, 640);

        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class CarouselBlockValidator : AbstractValidator<CarouselBlock>
{
    public CarouselBlockValidator()
    {
        RuleFor(b => b.Props.Slides.Count)
            .LessThanOrEqualTo(DocumentRules.MaxListItems).WithMessage($"A carousel can have at most {DocumentRules.MaxListItems} slides.");

        RuleForEach(b => b.Props.Slides).ChildRules(slide =>
        {
            slide.RuleFor(s => s.Src)
                .Must(DocumentRules.IsImageSource).WithMessage("Carousel images must come from the media library.");

            slide.RuleFor(s => s.Caption).MaximumLength(DocumentRules.MaxTextLength);
        });

        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class FaqBlockValidator : AbstractValidator<FaqBlock>
{
    public FaqBlockValidator()
    {
        RuleFor(b => b.Props.Items.Count)
            .LessThanOrEqualTo(DocumentRules.MaxListItems).WithMessage($"An FAQ can have at most {DocumentRules.MaxListItems} questions.");

        RuleForEach(b => b.Props.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Question).MaximumLength(DocumentRules.MaxTextLength);

            item.RuleFor(i => i.Answer).MaximumLength(2_000);
        });
    }
}

public class TestimonialsBlockValidator : AbstractValidator<TestimonialsBlock>
{
    public TestimonialsBlockValidator()
    {
        RuleFor(b => b.Props.Items.Count)
            .LessThanOrEqualTo(DocumentRules.MaxListItems).WithMessage($"At most {DocumentRules.MaxListItems} testimonials.");

        RuleForEach(b => b.Props.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quote).MaximumLength(2_000);

            item.RuleFor(i => i.Name).MaximumLength(100);

            item.RuleFor(i => i.Role).MaximumLength(100);
        });

        RuleFor(b => b.Props.Columns).InclusiveBetween(1, 4);
    }
}

public class FeaturesBlockValidator : AbstractValidator<FeaturesBlock>
{
    public FeaturesBlockValidator()
    {
        RuleFor(b => b.Props.Items.Count)
            .LessThanOrEqualTo(DocumentRules.MaxListItems).WithMessage($"At most {DocumentRules.MaxListItems} features.");

        RuleForEach(b => b.Props.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Icon)
                .Must(DocumentRules.FeatureIcons.Contains).WithMessage("Unknown feature icon.");

            item.RuleFor(i => i.Title).MaximumLength(100);

            item.RuleFor(i => i.Text).MaximumLength(DocumentRules.MaxTextLength);
        });

        RuleFor(b => b.Props.Columns).InclusiveBetween(1, 4);
    }
}

public class NavBlockValidator : AbstractValidator<NavBlock>
{
    public NavBlockValidator()
    {
        RuleFor(b => b.Props.Logo).MaximumLength(100);

        RuleFor(b => b.Props.Links.Count)
            .LessThanOrEqualTo(DocumentRules.MaxListItems).WithMessage($"A menu can have at most {DocumentRules.MaxListItems} links.");

        RuleForEach(b => b.Props.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.Label).MaximumLength(100);

            link.RuleFor(l => l.Href)
                .MaximumLength(DocumentRules.MaxUrlLength)
                .Must(DocumentRules.IsLink).WithMessage("Menu links must start with https://, http://, mailto:, tel:, / or #.");
        });

        RuleFor(b => b.Props.CtaLabel).MaximumLength(100);

        RuleFor(b => b.Props.CtaHref)
            .MaximumLength(DocumentRules.MaxUrlLength)
            .Must(DocumentRules.IsLink).WithMessage("The menu button's link must start with https://, http://, mailto:, tel:, / or #.");
    }
}
