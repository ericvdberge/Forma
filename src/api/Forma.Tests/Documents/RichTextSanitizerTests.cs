using Forma.Application.Documents;

namespace Forma.Tests.Documents;

public class RichTextSanitizerTests
{
    private readonly RichTextSanitizer _sanitizer = new();

    /// <summary>Markup as Tiptap's getHTML() writes it for the studio's extensions.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("<p></p>")]
    [InlineData("<p>Build beautiful <span data-muted=\"\">experiences.</span></p>")]
    [InlineData("<p><strong>Bold</strong>, <em>italic</em>, <u>underline</u> and <s>strike</s></p>")]
    [InlineData("<p>Line one<br>line two</p>")]
    [InlineData("<p>Tom &amp; Jerry&nbsp;say &lt;hi&gt; — “quoted” 'single'</p>")]
    [InlineData("<p><a rel=\"noopener\" href=\"https://example.com/?a=1&amp;b=2\">link</a> and <a rel=\"noopener\" href=\"/about\">page</a></p>")]
    [InlineData("<ul><li><p>One</p></li><li><p>Two</p></li></ul><ol start=\"3\"><li><p>Three</p></li></ol>")]
    [InlineData("<blockquote><p>Quote</p></blockquote>")]
    public void Accepts_what_the_studio_writes(string html)
    {
        Assert.True(_sanitizer.IsClean(html), _sanitizer.Sanitize(html));
    }

    [Theory]
    [InlineData("<p>Hi<script>alert(1)</script></p>")]
    [InlineData("<p class=\"x\">Hi</p>")]
    [InlineData("<h1>Hi</h1>")]
    [InlineData("<p><a href=\"javascript:alert(1)\">x</a></p>")]
    [InlineData("<p><a href=\"https://example.com\" target=\"_blank\">x</a></p>")]
    [InlineData("<iframe src=\"https://example.com\"></iframe>")]
    public void Rejects_anything_else(string html)
    {
        Assert.False(_sanitizer.IsClean(html));
    }

    [Fact]
    public void Sanitize_keeps_text_and_drops_unsafe_markup()
    {
        Assert.Equal("<p>Hi</p>", _sanitizer.Sanitize("<p onclick=\"x()\">Hi<script>alert(1)</script></p>"));
    }
}
