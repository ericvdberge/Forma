using System.Text.Json.Serialization;

namespace Forma.Application.Documents;

/// <summary>A full-width band of the page holding blocks.</summary>
public sealed record Section
{
    /// <summary>Id the studio created, e.g. <c>s_ab12cd34</c>; unique within the document.</summary>
    public required string Id { get; init; }

    public required SectionSettings Settings { get; init; }

    /// <summary>The section's blocks in order. In a split layout, blocks from <see cref="SectionSettings.SplitAt"/> go into the second column.</summary>
    public required IReadOnlyList<Block> Blocks { get; init; }
}

public sealed record SectionSettings
{
    public required SectionType Type { get; init; }

    /// <summary>Shown in the layers list; empty falls back to the section type.</summary>
    public required string Name { get; init; }

    public required SectionLayout Layout { get; init; }

    /// <summary>Split layout: share of the width the first column takes, in % (20–80).</summary>
    public required int SplitRatio { get; init; }

    /// <summary>Split layout: index of the first block that goes into the second column.</summary>
    public required int SplitAt { get; init; }

    public required VerticalAlign VerticalAlign { get; init; }

    public required ResponsiveSpacing Padding { get; init; }

    public required Background Background { get; init; }

    public required ContentWidth ContentWidth { get; init; }

    public required SectionAlign Align { get; init; }

    public required SectionTheme Theme { get; init; }

    /// <summary>px, 0 = auto.</summary>
    public required int MinHeight { get; init; }

    /// <summary>px between blocks.</summary>
    public required int Gap { get; init; }

    /// <summary>px; rounds the section's corners.</summary>
    public required int Radius { get; init; }

    public required SectionAnimation Animation { get; init; }

    public required string Anchor { get; init; }

    public required string ClassName { get; init; }

    public required Visibility Visibility { get; init; }
}

/// <summary>Padding set for desktop; tablet and mobile override single sides and inherit the rest from the next larger screen.</summary>
public sealed record ResponsiveSpacing
{
    public required Spacing Desktop { get; init; }

    public SpacingOverride? Tablet { get; init; }

    public SpacingOverride? Mobile { get; init; }
}

public sealed record Spacing
{
    public required int Top { get; init; }
    public required int Bottom { get; init; }
    public required int Left { get; init; }
    public required int Right { get; init; }
}

/// <summary>Sides that are not set inherit from the next larger screen.</summary>
public sealed record SpacingOverride
{
    public int? Top { get; init; }
    public int? Bottom { get; init; }
    public int? Left { get; init; }
    public int? Right { get; init; }
}

public sealed record Background
{
    public required BackgroundKind Kind { get; init; }

    public required string Color { get; init; }

    /// <summary>An image source: empty, <c>/media/{id}/image</c> or a built-in <c>/media/{name}.svg</c>.</summary>
    public required string Image { get; init; }

    /// <summary>Where the image is anchored, e.g. <c>right center</c>.</summary>
    public required string Position { get; init; }

    public required string GradientFrom { get; init; }

    public required string GradientTo { get; init; }

    public required int GradientAngle { get; init; }

    public required string OverlayColor { get; init; }

    /// <summary>0–100.</summary>
    public required int OverlayOpacity { get; init; }
}

public sealed record SectionAnimation
{
    public required EntranceAnimation Entrance { get; init; }

    /// <summary>Seconds, 0–2.</summary>
    public required double Delay { get; init; }
}

public sealed record Visibility
{
    public required bool Desktop { get; init; }
    public required bool Tablet { get; init; }
    public required bool Mobile { get; init; }
}

public enum SectionType { Hero, Content, Features, Split, Cta, Header, Footer, Sidebar }

public enum SectionLayout { Stack, Split }

public enum VerticalAlign { Start, Center, End }

public enum ContentWidth { Narrow, Normal, Wide, Full }

public enum SectionAlign { Left, Center }

public enum SectionTheme { Light, Dark }

public enum BackgroundKind { None, Color, Image, Gradient }

public enum EntranceAnimation
{
    None,
    Fade,
    [JsonStringEnumMemberName("fade-up")] FadeUp,
    Zoom,
    [JsonStringEnumMemberName("slide-left")] SlideLeft,
}
