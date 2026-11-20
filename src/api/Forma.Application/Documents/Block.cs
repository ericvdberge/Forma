using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forma.Application.Documents;

/// <summary>
/// One piece of content in a section. <c>type</c> picks the derived record and so the shape of <c>props</c>;
/// an unknown type fails deserialisation. The studio writes <c>id</c> before <c>type</c>, which is why
/// <see cref="PageDocumentJson"/> allows out-of-order metadata.
/// </summary>
/// <remarks>
/// <c>props</c> holds literal values only. Collection bindings (future) go in a separate <c>bindings</c>
/// property, so these records don't change: see <c>docs/design/collections.md</c>.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HeadingBlock), "heading")]
[JsonDerivedType(typeof(TextBlock), "text")]
[JsonDerivedType(typeof(ButtonBlock), "button")]
[JsonDerivedType(typeof(BadgeBlock), "badge")]
[JsonDerivedType(typeof(ImageBlock), "image")]
[JsonDerivedType(typeof(GalleryBlock), "gallery")]
[JsonDerivedType(typeof(VideoBlock), "video")]
[JsonDerivedType(typeof(DividerBlock), "divider")]
[JsonDerivedType(typeof(SpacerBlock), "spacer")]
[JsonDerivedType(typeof(FormBlock), "form")]
[JsonDerivedType(typeof(MapBlock), "map")]
[JsonDerivedType(typeof(CarouselBlock), "carousel")]
[JsonDerivedType(typeof(FaqBlock), "faq")]
[JsonDerivedType(typeof(TestimonialsBlock), "testimonials")]
[JsonDerivedType(typeof(FeaturesBlock), "features")]
[JsonDerivedType(typeof(NavBlock), "nav")]
[JsonDerivedType(typeof(PluginBlock), "plugin")]
public abstract record Block
{
    /// <summary>Id the studio created, e.g. <c>b_ab12cd34</c>; unique within the document.</summary>
    public required string Id { get; init; }

    public required BlockStyle Style { get; init; }
}

public sealed record BlockStyle
{
    public required BlockAlign Align { get; init; }
    public required int MarginTop { get; init; }
    public required int MarginBottom { get; init; }
    public required BlockMaxWidth MaxWidth { get; init; }
    public required string Anchor { get; init; }
    public required string ClassName { get; init; }
    public required Visibility Visibility { get; init; }
}

public enum BlockAlign { Inherit, Left, Center, Right }

public enum BlockMaxWidth { None, Sm, Md, Lg }

// ── Block types ────────────────────────────────────────────────────────────

public sealed record HeadingBlock : Block { public required HeadingProps Props { get; init; } }
public sealed record TextBlock : Block { public required TextProps Props { get; init; } }
public sealed record ButtonBlock : Block { public required ButtonProps Props { get; init; } }
public sealed record BadgeBlock : Block { public required BadgeProps Props { get; init; } }
public sealed record ImageBlock : Block { public required ImageProps Props { get; init; } }
public sealed record GalleryBlock : Block { public required GalleryProps Props { get; init; } }
public sealed record VideoBlock : Block { public required VideoProps Props { get; init; } }
public sealed record DividerBlock : Block { public required DividerProps Props { get; init; } }
public sealed record SpacerBlock : Block { public required SpacerProps Props { get; init; } }
public sealed record FormBlock : Block { public required FormProps Props { get; init; } }
public sealed record MapBlock : Block { public required MapProps Props { get; init; } }
public sealed record CarouselBlock : Block { public required CarouselProps Props { get; init; } }
public sealed record FaqBlock : Block { public required FaqProps Props { get; init; } }
public sealed record TestimonialsBlock : Block { public required TestimonialsProps Props { get; init; } }
public sealed record FeaturesBlock : Block { public required FeaturesProps Props { get; init; } }
public sealed record NavBlock : Block { public required NavProps Props { get; init; } }

/// <summary>A block a plugin declared in its manifest (<c>blocks</c>); the studio draws it from the plugin's template.</summary>
public sealed record PluginBlock : Block { public required PluginBlockProps Props { get; init; } }

// ── Props ──────────────────────────────────────────────────────────────────

/// <param name="Html">Rich text; only the studio's Tiptap markup is allowed.</param>
/// <param name="Level">HTML heading level, 1–4.</param>
public sealed record HeadingProps(string Html, int Level, HeadingSize Size);

public sealed record TextProps(string Html, TextSize Size, TextTone Tone);

public sealed record ButtonProps(string Label, string Href, ButtonVariant Variant, bool Arrow, ButtonSize Size);

public sealed record BadgeProps(string Text);

/// <param name="Src">An image source: empty, <c>/media/{id}/image</c> or a built-in <c>/media/{name}.svg</c>.</param>
public sealed record ImageProps(string Src, string Alt, ImageAspect Aspect, int Radius, string Caption);

public sealed record GalleryProps(IReadOnlyList<GalleryImage> Images, int Columns, int Gap, int Radius);

public sealed record GalleryImage(string Src, string Alt);

/// <param name="Url">A YouTube or Vimeo link, or empty.</param>
public sealed record VideoProps(string Url, VideoAspect Aspect, int Radius);

public sealed record DividerProps(DividerStyle Style, int Thickness);

public sealed record SpacerProps(int Height);

public sealed record FormProps(IReadOnlyList<FormField> Fields, string SubmitLabel, string SuccessMessage);

public sealed record FormField(string Label, FormFieldType Type, bool Required);

public sealed record MapProps(string Address, int Zoom, int Height, int Radius);

public sealed record CarouselProps(IReadOnlyList<CarouselSlide> Slides, bool Autoplay, CarouselAspect Aspect, int Radius);

public sealed record CarouselSlide(string Src, string Caption);

public sealed record FaqProps(IReadOnlyList<FaqItem> Items);

public sealed record FaqItem(string Question, string Answer);

public sealed record TestimonialsProps(IReadOnlyList<Testimonial> Items, int Columns);

public sealed record Testimonial(string Quote, string Name, string Role);

public sealed record FeaturesProps(IReadOnlyList<FeatureItem> Items, int Columns, bool Card);

/// <param name="Icon">Name of one of the studio's feature icons, e.g. <c>wand</c>.</param>
public sealed record FeatureItem(string Icon, string Title, string Text);

public sealed record NavProps(
    string Logo,
    IReadOnlyList<NavLink> Links,
    string CtaLabel,
    string CtaHref,
    NavOrientation Orientation,
    bool Card);

/// <param name="Plugin">The id of the plugin that declared the block, e.g. <c>product-card</c>.</param>
/// <param name="Block">The block's id within that plugin.</param>
/// <param name="Values">What people filled in, by field key: strings, numbers and booleans only.</param>
/// <remarks>
/// The values aren't checked against the plugin's fields here: a page must stay saveable when the plugin is updated or
/// removed. The publish check warns about blocks whose plugin isn't installed.
/// </remarks>
public sealed record PluginBlockProps(string Plugin, string Block, IReadOnlyDictionary<string, JsonElement> Values);

/// <param name="Menu">Shows a dropdown chevron.</param>
public sealed record NavLink(string Label, string Href, bool? Menu = null);

// ── Prop enums ─────────────────────────────────────────────────────────────

public enum HeadingSize { Xl, Lg, Md, Sm }

public enum TextSize { Lg, Md, Sm }

public enum TextTone { Default, Muted }

public enum ButtonVariant { Primary, Secondary, Accent }

public enum ButtonSize { Sm, Md, Lg }

public enum ImageAspect
{
    Auto,
    [JsonStringEnumMemberName("16/9")] Wide16x9,
    [JsonStringEnumMemberName("4/3")] Standard4x3,
    [JsonStringEnumMemberName("1/1")] Square1x1,
    [JsonStringEnumMemberName("3/4")] Portrait3x4,
}

public enum VideoAspect
{
    [JsonStringEnumMemberName("16/9")] Wide16x9,
    [JsonStringEnumMemberName("4/3")] Standard4x3,
    [JsonStringEnumMemberName("1/1")] Square1x1,
}

public enum CarouselAspect
{
    [JsonStringEnumMemberName("16/9")] Wide16x9,
    [JsonStringEnumMemberName("4/3")] Standard4x3,
    [JsonStringEnumMemberName("21/9")] Ultrawide21x9,
}

public enum DividerStyle { Solid, Dashed }

public enum FormFieldType { Text, Email, Textarea }

public enum NavOrientation { Horizontal, Vertical }
