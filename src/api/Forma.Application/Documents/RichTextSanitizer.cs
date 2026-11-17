using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace Forma.Application.Documents;

/// <summary>
/// <see cref="IRichTextSanitizer"/> with the markup the studio's Tiptap setup (<c>src/app/src/studio/tiptap.ts</c>) produces:
/// StarterKit's paragraphs, line breaks, bold, italic, underline, strikethrough, lists, quotes and links, plus the
/// <c>span[data-muted]</c> mark.
/// </summary>
public sealed class RichTextSanitizer : IRichTextSanitizer
{
    private static readonly string[] AllowedTags = ["p", "br", "strong", "em", "u", "s", "a", "ul", "ol", "li", "blockquote", "span"];
    private static readonly string[] AllowedAttributes = ["href", "rel", "data-muted", "start"];
    private static readonly string[] AllowedSchemes = ["http", "https", "mailto", "tel"];

    private static readonly IMarkupFormatter Formatter = HtmlMarkupFormatter.Instance;

    private readonly HtmlSanitizer _sanitizer;
    private readonly HtmlParser _parser = new();

    public RichTextSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(AllowedTags, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(AllowedAttributes, StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(AllowedSchemes, StringComparer.OrdinalIgnoreCase),
            UriAttributes = new HashSet<string>(["href"], StringComparer.OrdinalIgnoreCase),
            AllowedCssProperties = new HashSet<string>(),
            AllowedAtRules = new HashSet<AngleSharp.Css.Dom.CssRuleType>(),
            AllowedClasses = new HashSet<string>(),
        };
        // Disallowed elements go with their content (a <script>'s text must not end up as visible text).
        _sanitizer = new HtmlSanitizer(options);
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html, outputFormatter: Formatter);

    /// <remarks>
    /// Compares parsed markup, not the raw strings: the studio and AngleSharp may write the same markup differently
    /// (entities, attribute quoting), and only a real change by the sanitiser should count.
    /// </remarks>
    public bool IsClean(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return true;
        }

        return Normalize(html) == Sanitize(html);
    }

    private string Normalize(string html)
    {
        var document = _parser.ParseDocument("<!doctype html><html><body></body></html>");
        var nodes = _parser.ParseFragment(html, document.Body!);
        return string.Concat(nodes.Select(n => n.ToHtml(Formatter)));
    }
}
