using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Forma.Infrastructure.Plugins;

/// <summary>
/// A block a plugin adds to the studio's library, declared in <c>plugin.json</c> under <c>blocks</c>. People fill in its
/// <see cref="Fields"/> in the inspector; the studio draws it from <see cref="Template"/>. Specification:
/// <c>docs/design/plugins.md</c> (“Blocks”).
/// </summary>
/// <param name="Id">Unique within the plugin; same rules as a plugin id, e.g. <c>product-card</c>.</param>
/// <param name="Label">Shown in the library, e.g. "Product card".</param>
/// <param name="Template">
/// Restricted HTML with <c>{{key}}</c> slots for the field values; see <see cref="PluginBlockTemplate"/> for what it may contain.
/// </param>
/// <param name="Fields">What people fill in, in inspector order.</param>
/// <param name="Description">One line for the library's tooltip.</param>
/// <param name="Icon">A Lucide icon name for the library tile, e.g. <c>shopping-bag</c>; the studio falls back to a puzzle piece.</param>
public sealed record PluginBlockDefinition(
    string Id,
    string Label,
    string Template,
    IReadOnlyList<PluginBlockField> Fields,
    string? Description = null,
    string? Icon = null);

/// <summary>One inspector field of a plugin block. Its value is stored in the block under <c>props.values[key]</c>.</summary>
/// <param name="Key">The value's name, used as <c>{{key}}</c> in the template.</param>
/// <param name="Kind">One of <see cref="PluginBlockFieldKinds.All"/>: the same controls the built-in blocks use.</param>
/// <param name="Default">The value of a new block; a string, number or boolean.</param>
/// <param name="Options">The choices of a <c>select</c> or <c>segmented</c> field.</param>
/// <param name="Min">Lowest value of a <c>number</c> or <c>slider</c> field.</param>
/// <param name="Max">Highest value of a <c>number</c> or <c>slider</c> field.</param>
/// <param name="Advanced">Hidden in the studio's Simple mode.</param>
public sealed record PluginBlockField(
    string Key,
    string Label,
    string Kind,
    JsonElement? Default = null,
    IReadOnlyList<PluginBlockFieldOption>? Options = null,
    string? Placeholder = null,
    double? Min = null,
    double? Max = null,
    double? Step = null,
    string? Unit = null,
    bool Advanced = false);

public sealed record PluginBlockFieldOption(string Value, string Label);

public static class PluginBlockFieldKinds
{
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(["text", "textarea", "url", "image", "color", "number", "slider", "toggle", "select", "segmented"]);

    public static readonly IReadOnlySet<string> WithOptions = new HashSet<string>(["select", "segmented"]);

    public static readonly IReadOnlySet<string> WithRange = new HashSet<string>(["number", "slider"]);
}

/// <summary>
/// The rules for a plugin block's template, checked when the plugin loads. A template is a small piece of HTML from a
/// fixed set of tags, with no scripts, event handlers, classes or embedded resources:
/// <list type="bullet">
/// <item>tags: layout and text (<c>div</c>, <c>p</c>, <c>h2</c>–<c>h4</c>, lists…), plus <c>a</c> and <c>img</c>;</item>
/// <item>attributes: <c>style</c>, <c>href</c>, <c>src</c>, <c>alt</c>, <c>title</c> and <c>data-if</c> (drop the
/// element when that field is empty or off);</item>
/// <item><c>href</c> and <c>src</c> are a single <c>{{key}}</c> slot or a fixed https, relative or <c>/media/</c> address;</item>
/// <item><c>style</c> can't load anything (<c>url()</c>, <c>@import</c>, escapes).</item>
/// </list>
/// The studio checks the same rules again when it draws the block, and only lets safe values into <c>href</c>,
/// <c>src</c> and <c>style</c> slots.
/// </summary>
public static partial class PluginBlockTemplate
{
    public const int MaxLength = 16 * 1024;

    public static readonly IReadOnlySet<string> AllowedTags = new HashSet<string>(
        ["div", "span", "p", "h2", "h3", "h4", "strong", "em", "small", "s", "br", "hr", "img", "a", "ul", "ol", "li", "figure", "figcaption"],
        StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> AllowedAttributes = new HashSet<string>(
        ["style", "href", "src", "alt", "title", "data-if"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HtmlParser Parser = new();

    /// <summary>The problem with the template, or null when it is fine.</summary>
    /// <param name="keys">The block's field keys; every <c>{{key}}</c> must be one of them.</param>
    public static string? Problem(string template, IReadOnlySet<string> keys)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return "the template is empty";
        }

        if (template.Length > MaxLength)
        {
            return $"the template is longer than {MaxLength / 1024} KB";
        }

        foreach (Match slot in SlotPattern().Matches(template))
        {
            if (!keys.Contains(slot.Groups[1].Value))
            {
                return $"the template uses {{{{{slot.Groups[1].Value}}}}}, which isn't one of its fields";
            }
        }

        var body = Parser.ParseDocument($"<!doctype html><html><body>{template}</body></html>").Body!;
        foreach (var element in body.Descendants<IElement>())
        {
            if (!AllowedTags.Contains(element.LocalName))
            {
                return $"<{element.LocalName}> isn't allowed in a template";
            }

            foreach (var attribute in element.Attributes)
            {
                if (AttributeProblem(element.LocalName, attribute.Name, attribute.Value, keys) is { } problem)
                {
                    return problem;
                }
            }
        }

        return null;
    }

    private static string? AttributeProblem(string tag, string name, string value, IReadOnlySet<string> keys)
    {
        if (!AllowedAttributes.Contains(name))
        {
            return $"the attribute '{name}' isn't allowed in a template";
        }

        switch (name.ToLowerInvariant())
        {
            case "href" when tag != "a":
            case "src" when tag != "img":
                return $"'{name}' is only allowed on <{(name == "href" ? "a" : "img")}>";
            case "href" or "src" when !IsSingleSlot(value) && !IsFixedAddress(value):
                return $"'{name}' must be a single {{{{key}}}} slot or an https://, relative or /media/ address";
            case "style" when UnsafeCss().IsMatch(value):
                return "a 'style' can't load anything (url(), @import, expression() or escapes)";
            case "data-if" when !keys.Contains(value):
                return $"data-if=\"{value}\" isn't one of the block's fields";
            default:
                return null;
        }
    }

    private static bool IsSingleSlot(string value) => SingleSlotPattern().IsMatch(value);

    private static bool IsFixedAddress(string value) =>
        !value.Contains("{{", StringComparison.Ordinal)
        && (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith('#')
            || (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal)));

    [GeneratedRegex(@"\{\{\s*([^{}\s]*)\s*\}\}")]
    private static partial Regex SlotPattern();

    [GeneratedRegex(@"^\{\{\s*[a-zA-Z][a-zA-Z0-9_]{0,39}\s*\}\}$")]
    private static partial Regex SingleSlotPattern();

    [GeneratedRegex(@"url\s*\(|@import|expression\s*\(|\\|javascript:|<", RegexOptions.IgnoreCase)]
    private static partial Regex UnsafeCss();
}
