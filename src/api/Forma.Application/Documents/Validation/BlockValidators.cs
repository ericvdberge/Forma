using FluentValidation;

namespace Forma.Application.Documents.Validation;

// Props rules per block type. Ranges mirror the inspector fields in src/app/src/studio/blocks.ts.

public class HeadingBlockValidator : AbstractValidator<HeadingBlock>
{
    public HeadingBlockValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(b => b.Props.Html).RichText(sanitizer);
        RuleFor(b => b.Props.Level).InclusiveBetween(1, 4);
    }
}

public class TextBlockValidator : AbstractValidator<TextBlock>
{
    public TextBlockValidator(IRichTextSanitizer sanitizer)
    {
        RuleFor(b => b.Props.Html).RichText(sanitizer);
    }
}

public class ButtonBlockValidator : AbstractValidator<ButtonBlock>
{
    public ButtonBlockValidator()
    {
        RuleFor(b => b.Props.Label).PlainText(100);
        RuleFor(b => b.Props.Href).Link();
    }
}

public class BadgeBlockValidator : AbstractValidator<BadgeBlock>
{
    public BadgeBlockValidator()
    {
        RuleFor(b => b.Props.Text).PlainText(100);
    }
}

public class ImageBlockValidator : AbstractValidator<ImageBlock>
{
    public ImageBlockValidator()
    {
        RuleFor(b => b.Props.Src).ImageSource();
        RuleFor(b => b.Props.Alt).PlainText();
        RuleFor(b => b.Props.Caption).PlainText();
        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class GalleryBlockValidator : AbstractValidator<GalleryBlock>
{
    public GalleryBlockValidator()
    {
        RuleFor(b => b.Props.Images).ListLimit();
        RuleForEach(b => b.Props.Images).ChildRules(image =>
        {
            image.RuleFor(i => i.Src).ImageSource();
            image.RuleFor(i => i.Alt).PlainText();
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
        RuleFor(b => b.Props.Fields).ListLimit();
        RuleForEach(b => b.Props.Fields).ChildRules(field => field.RuleFor(f => f.Label).PlainText(100));
        RuleFor(b => b.Props.SubmitLabel).PlainText(100);
        RuleFor(b => b.Props.SuccessMessage).PlainText();
    }
}

public class MapBlockValidator : AbstractValidator<MapBlock>
{
    public MapBlockValidator()
    {
        RuleFor(b => b.Props.Address).PlainText(200);
        RuleFor(b => b.Props.Zoom).InclusiveBetween(3, 19);
        RuleFor(b => b.Props.Height).InclusiveBetween(160, 640);
        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class CarouselBlockValidator : AbstractValidator<CarouselBlock>
{
    public CarouselBlockValidator()
    {
        RuleFor(b => b.Props.Slides).ListLimit();
        RuleForEach(b => b.Props.Slides).ChildRules(slide =>
        {
            slide.RuleFor(s => s.Src).ImageSource();
            slide.RuleFor(s => s.Caption).PlainText();
        });
        RuleFor(b => b.Props.Radius).InclusiveBetween(0, 40);
    }
}

public class FaqBlockValidator : AbstractValidator<FaqBlock>
{
    public FaqBlockValidator()
    {
        RuleFor(b => b.Props.Items).ListLimit();
        RuleForEach(b => b.Props.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Question).PlainText();
            item.RuleFor(i => i.Answer).PlainText(2_000);
        });
    }
}

public class TestimonialsBlockValidator : AbstractValidator<TestimonialsBlock>
{
    public TestimonialsBlockValidator()
    {
        RuleFor(b => b.Props.Items).ListLimit();
        RuleForEach(b => b.Props.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quote).PlainText(2_000);
            item.RuleFor(i => i.Name).PlainText(100);
            item.RuleFor(i => i.Role).PlainText(100);
        });
        RuleFor(b => b.Props.Columns).InclusiveBetween(1, 4);
    }
}

public class FeaturesBlockValidator : AbstractValidator<FeaturesBlock>
{
    public FeaturesBlockValidator()
    {
        RuleFor(b => b.Props.Items).ListLimit();
        RuleForEach(b => b.Props.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Icon).Must(DocumentRules.FeatureIcons.Contains).WithMessage("Unknown icon.");
            item.RuleFor(i => i.Title).PlainText(100);
            item.RuleFor(i => i.Text).PlainText();
        });
        RuleFor(b => b.Props.Columns).InclusiveBetween(1, 4);
    }
}

public class NavBlockValidator : AbstractValidator<NavBlock>
{
    public NavBlockValidator()
    {
        RuleFor(b => b.Props.Logo).PlainText(100);
        RuleFor(b => b.Props.Links).ListLimit();
        RuleForEach(b => b.Props.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.Label).PlainText(100);
            link.RuleFor(l => l.Href).Link();
        });
        RuleFor(b => b.Props.CtaLabel).PlainText(100);
        RuleFor(b => b.Props.CtaHref).Link();
    }
}
