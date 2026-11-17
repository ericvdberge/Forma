using System.Text.Json;
using Forma.Application;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Documents;

public class PageDocumentValidatorTests
{
    private static readonly PageDocumentValidator Validator;

    static PageDocumentValidatorTests()
    {
        // Sets the camelCase property name resolver the API uses.
        new ServiceCollection().AddApplication();
        Validator = new PageDocumentValidator(new RichTextSanitizer());
    }

    private static PageDocument Templates() =>
        JsonSerializer.Deserialize<PageDocument>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "Studio", "templates.json")),
            PageDocumentJson.Options)!;

    private static IReadOnlyDictionary<string, string> Errors(PageDocument document) =>
        Validator.Validate(document).Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.First().ErrorMessage);

    private static PageDocument WithFirstBlock(PageDocument document, Block block)
    {
        var section = document.Sections[0];
        return document with { Sections = [section with { Blocks = [block, .. section.Blocks.Skip(1)] }, .. document.Sections.Skip(1)] };
    }

    private static T FirstBlock<T>(PageDocument document) where T : Block =>
        document.Sections.SelectMany(s => s.Blocks).OfType<T>().First();

    private static BlockStyle Style() => FirstBlock<Block>(Templates()).Style;

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hi")]
    [InlineData("//evil.example")]
    [InlineData("ftp://files.example")]
    public void Rejects_unsafe_links(string href)
    {
        var button = new ButtonBlock { Id = "b_x", Style = Style(), Props = new ButtonProps("Go", href, ButtonVariant.Primary, true, ButtonSize.Md) };

        var errors = Errors(WithFirstBlock(Templates(), button));

        Assert.Contains("sections[0].blocks[0].props.href", errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#pricing")]
    [InlineData("/about")]
    [InlineData("https://example.com/a?b=c")]
    [InlineData("mailto:hello@example.com")]
    [InlineData("tel:+31612345678")]
    public void Accepts_safe_links(string href)
    {
        var button = new ButtonBlock { Id = "b_x", Style = Style(), Props = new ButtonProps("Go", href, ButtonVariant.Primary, true, ButtonSize.Md) };

        Assert.True(Validator.Validate(WithFirstBlock(Templates(), button)).IsValid);
    }

    [Theory]
    [InlineData("<p>Hi<script>alert(1)</script></p>")]
    [InlineData("<p onclick=\"alert(1)\">Hi</p>")]
    [InlineData("<p style=\"color:red\">Hi</p>")]
    [InlineData("<p><a href=\"javascript:alert(1)\">Hi</a></p>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    public void Rejects_markup_outside_the_allowlist(string html)
    {
        var heading = new HeadingBlock { Id = "b_x", Style = Style(), Props = new HeadingProps(html, 2, HeadingSize.Lg) };

        var errors = Errors(WithFirstBlock(Templates(), heading));

        Assert.Equal("The text contains markup that isn't allowed.", errors["sections[0].blocks[0].props.html"]);
    }

    [Fact]
    public void Rejects_absolute_image_urls()
    {
        var image = new ImageBlock
        {
            Id = "b_x",
            Style = Style(),
            Props = new ImageProps("http://localhost:5134/media/0b6c0f6e-0000-4000-8000-000000000000/image", "", ImageAspect.Auto, 0, ""),
        };

        Assert.Contains("sections[0].blocks[0].props.src", Errors(WithFirstBlock(Templates(), image)).Keys);
    }

    [Fact]
    public void Accepts_media_library_and_built_in_images()
    {
        var image = new ImageBlock
        {
            Id = "b_x",
            Style = Style(),
            Props = new ImageProps("/media/0b6c0f6e-0000-4000-8000-000000000000/image", "", ImageAspect.Auto, 0, ""),
        };

        Assert.True(Validator.Validate(WithFirstBlock(Templates(), image)).IsValid);
        Assert.True(DocumentRules.IsImageSource("/media/hero.svg"));
    }

    [Fact]
    public void Rejects_duplicate_ids()
    {
        var document = Templates();
        var copy = document.Sections[0] with { };

        var errors = Errors(document with { Sections = [.. document.Sections, copy] });

        Assert.Contains($"sections[{document.Sections.Count}].id", errors.Keys);
    }

    [Fact]
    public void Rejects_values_outside_the_inspector_ranges()
    {
        var document = Templates();
        var section = document.Sections[0];
        var broken = section with { Settings = section.Settings with { SplitRatio = 95, Radius = -1 } };

        var errors = Errors(document with { Sections = [broken, .. document.Sections.Skip(1)] });

        Assert.Contains("sections[0].settings.splitRatio", errors.Keys);
        Assert.Contains("sections[0].settings.radius", errors.Keys);
    }

    [Fact]
    public void Rejects_css_in_colours()
    {
        var document = Templates();
        var section = document.Sections[0];
        var broken = section with
        {
            Settings = section.Settings with { Background = section.Settings.Background with { Color = "red;background:url(x)" } },
        };

        var errors = Errors(document with { Sections = [broken, .. document.Sections.Skip(1)] });

        Assert.Contains("sections[0].settings.background.color", errors.Keys);
    }
}
