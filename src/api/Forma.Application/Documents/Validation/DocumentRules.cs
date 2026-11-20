using System.Text.RegularExpressions;

namespace Forma.Application.Documents.Validation;

/// <summary>
/// Limits and value checks shared by the page document validators (and later saved items and collection values).
/// Limits mirror the studio's inspector controls; see <c>docs/design/page-content.md</c> § 6.
/// </summary>
public static partial class DocumentRules
{
    public const int MaxSections = 100;
    public const int MaxBlocksPerSection = 60;
    public const int MaxListItems = 50;
    public const int MaxTextLength = 500;
    public const int MaxHtmlLength = 20_000;
    public const int MaxUrlLength = 2_000;
    public const int MaxClassNameLength = 200;
    public const int MaxDocumentBytes = 512 * 1024;

    /// <summary>Icons a Features item can show (<c>featureIcons</c> in <c>src/app/src/studio/blocks.ts</c>).</summary>
    public static readonly IReadOnlySet<string> FeatureIcons =
        new HashSet<string>(["wand", "layers", "zap", "scale", "shield", "globe", "palette", "heart", "rocket", "users"]);

    [GeneratedRegex("^[a-z]+_[A-Za-z0-9_-]{1,40}$")]
    private static partial Regex IdPattern();

    [GeneratedRegex("^#([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial Regex ColorPattern();

    [GeneratedRegex("^(left|center|right|top|bottom)( (left|center|right|top|bottom))?$")]
    private static partial Regex PositionPattern();

    [GeneratedRegex("^[a-z0-9-]{0,60}$")]
    private static partial Regex AnchorPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]+( [A-Za-z0-9_-]+)*$")]
    private static partial Regex ClassNamePattern();

    [GeneratedRegex(@"^/media/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/image$")]
    private static partial Regex MediaImagePattern();

    [GeneratedRegex("^/media/[a-z0-9-]{1,60}\\.svg$")]
    private static partial Regex BuiltInImagePattern();

    [GeneratedRegex(@"^https://(www\.)?(youtube\.com/(watch\?v=|embed/|shorts/)|youtu\.be/|(player\.)?vimeo\.com/)[^\s]+$")]
    private static partial Regex VideoPattern();

    public static bool IsId(string value) => IdPattern().IsMatch(value);

    public static bool IsColor(string value) => ColorPattern().IsMatch(value);

    public static bool IsPosition(string value) => PositionPattern().IsMatch(value);

    public static bool IsAnchor(string value) => AnchorPattern().IsMatch(value);

    public static bool IsClassName(string value) => value.Length == 0 || ClassNamePattern().IsMatch(value);

    /// <summary>Empty, a media item (<c>/media/{id}/image</c>) or a built-in demo image (<c>/media/{name}.svg</c>).</summary>
    public static bool IsImageSource(string value) =>
        value.Length == 0 || MediaImagePattern().IsMatch(value) || BuiltInImagePattern().IsMatch(value);

    /// <summary>Empty or a YouTube / Vimeo link.</summary>
    public static bool IsVideoUrl(string value) => value.Length == 0 || VideoPattern().IsMatch(value);

    /// <summary>Empty, <c>#anchor</c>, <c>/relative</c>, <c>http(s)://</c>, <c>mailto:</c> or <c>tel:</c>.</summary>
    public static bool IsLink(string value)
    {
        if (value.Length == 0 || value.StartsWith('#'))
            return !value.Any(char.IsWhiteSpace);

        // "//host" is protocol-relative: another site, so it must be written as https://.
        if (value.StartsWith('/'))
            return !value.StartsWith("//") && !value.Any(char.IsWhiteSpace);

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https" or "mailto" or "tel";
    }

    /// <summary>Gets the media item id out of an image source, or null for built-in images and empty sources.</summary>
    public static Guid? MediaId(string source) =>
        MediaImagePattern().IsMatch(source) ? Guid.Parse(source.AsSpan(7, 36)) : null;
}
