using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Tests.Builders;

namespace Forma.Tests.Documents;

public class PageDocumentValidatorTests
{
    private static readonly PageDocumentValidator Validator = new(new RichTextSanitizer());

    private static IReadOnlyDictionary<string, string> Errors(PageDocument document) =>
        Validator.Validate(document).Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.First().ErrorMessage);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hi")]
    [InlineData("//evil.example")]
    [InlineData("ftp://files.example")]
    public void Rejects_unsafe_links(string href)
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithButton("b_cta", "Go", href))
            .Build();

        Assert.Equal("Button links must start with https://, http://, mailto:, tel:, / or #.", Errors(document)["Sections[0].Blocks[0].Props.Href"]);
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
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithButton("b_cta", "Go", href))
            .Build();

        Assert.Empty(Errors(document));
    }

    [Theory]
    [InlineData("<p>Hi<script>alert(1)</script></p>")]
    [InlineData("<p onclick=\"alert(1)\">Hi</p>")]
    [InlineData("<p style=\"color:red\">Hi</p>")]
    [InlineData("<p><a href=\"javascript:alert(1)\">Hi</a></p>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    public void Rejects_markup_outside_the_allowlist(string html)
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithHeading("b_title", html))
            .Build();

        Assert.Equal("The heading contains markup that isn't allowed.", Errors(document)["Sections[0].Blocks[0].Props.Html"]);
    }

    [Fact]
    public void Rejects_absolute_image_urls()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithImage("b_photo", "http://localhost:5134/media/0b6c0f6e-0000-4000-8000-000000000000/image"))
            .Build();

        Assert.Equal("Images must come from the media library.", Errors(document)["Sections[0].Blocks[0].Props.Src"]);
    }

    [Theory]
    [InlineData("/media/0b6c0f6e-0000-4000-8000-000000000000/image")]
    [InlineData("/media/hero.svg")]
    public void Accepts_media_library_and_built_in_images(string src)
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithImage("b_photo", src))
            .Build();

        Assert.Empty(Errors(document));
    }

    [Fact]
    public void Rejects_duplicate_ids()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithText("b_same").WithText("b_same"))
            .Build();

        Assert.Equal("Every section and block needs its own id.", Assert.Single(Errors(document)).Value);
    }

    [Fact]
    public void Rejects_values_outside_the_inspector_ranges()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithSettings(s => s with { SplitRatio = 95, Radius = -1 }))
            .Build();

        Assert.Equal(["Sections[0].Settings.Radius", "Sections[0].Settings.SplitRatio"], Errors(document).Keys.Order());
    }

    [Fact]
    public void Rejects_css_in_colours()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithSettings(s => s with { Background = s.Background with { Color = "red;background:url(x)" } }))
            .Build();

        Assert.Equal("Colours must be hex, e.g. #1a1a1a.", Errors(document)["Sections[0].Settings.Background.Color"]);
    }

    [Fact]
    public void Rejects_bindings_on_props_that_cant_show_a_field()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithHeading(
                "b_title",
                bindings: new Dictionary<string, Binding> { ["level"] = new(BindingScope.Page, "fld_title") }))
            .Build();

        Assert.Equal("‘level’ can't be connected to a collection field.", Errors(document)["Sections[0].Blocks[0].bindings[0]"]);
    }

    [Fact]
    public void Accepts_bindings_on_bindable_props()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithImage(
                "b_cover",
                bindings: new Dictionary<string, Binding> { ["src"] = new(BindingScope.Page, "fld_cover") }))
            .Build();

        Assert.Empty(Errors(document));
    }
}
