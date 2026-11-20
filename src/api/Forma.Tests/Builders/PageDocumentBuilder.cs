using Forma.Application.Documents;

namespace Forma.Tests.Builders;

/// <summary>
/// Builds page documents for tests: every section and block starts with the studio's defaults, and a test only
/// sets what it is about.
/// </summary>
/// <example>
/// <code>
/// var document = new PageDocumentBuilder()
///     .WithSection("s_hero", section => section
///         .WithHeading("b_title", "&lt;p&gt;Welcome&lt;/p&gt;", level: 1)
///         .WithButton("b_cta", "Get started", "/about"))
///     .Build();
/// </code>
/// </example>
public class PageDocumentBuilder
{
    private readonly List<Section> _sections = [];

    public PageDocumentBuilder WithSection(string id, Action<SectionBuilder>? configure = null)
    {
        var builder = new SectionBuilder(id);
        configure?.Invoke(builder);
        _sections.Add(builder.Build());

        return this;
    }

    public PageDocument Build() => new() { Version = PageDocument.CurrentVersion, Sections = [.. _sections] };
}

public class SectionBuilder(string id)
{
    private readonly List<Block> _blocks = [];
    private SectionSettings _settings = DefaultSettings;

    public static SectionSettings DefaultSettings => new()
    {
        Type = SectionType.Content,
        Name = "",
        Layout = SectionLayout.Stack,
        SplitRatio = 50,
        SplitAt = 1,
        VerticalAlign = VerticalAlign.Center,
        Padding = new ResponsiveSpacing { Desktop = new Spacing { Top = 64, Bottom = 64, Left = 32, Right = 32 } },
        Background = new Background
        {
            Kind = BackgroundKind.None,
            Color = "#ffffff",
            Image = "",
            Position = "center",
            GradientFrom = "#f5f4f0",
            GradientTo = "#ffffff",
            GradientAngle = 180,
            OverlayColor = "#000000",
            OverlayOpacity = 0,
        },
        ContentWidth = ContentWidth.Normal,
        Align = SectionAlign.Left,
        Theme = SectionTheme.Light,
        MinHeight = 0,
        Gap = 20,
        Radius = 0,
        Animation = new SectionAnimation { Entrance = EntranceAnimation.None, Delay = 0 },
        Anchor = "",
        ClassName = "",
        Visibility = new Visibility { Desktop = true, Tablet = true, Mobile = true },
    };

    public static BlockStyle DefaultStyle => new()
    {
        Align = BlockAlign.Inherit,
        MarginTop = 0,
        MarginBottom = 0,
        MaxWidth = BlockMaxWidth.None,
        Anchor = "",
        ClassName = "",
        Visibility = new Visibility { Desktop = true, Tablet = true, Mobile = true },
    };

    public SectionBuilder WithSettings(Func<SectionSettings, SectionSettings> change)
    {
        _settings = change(_settings);
        return this;
    }

    public SectionBuilder WithBlock(Block block)
    {
        _blocks.Add(block);
        return this;
    }

    public SectionBuilder WithHeading(string blockId, string html = "<p>Heading</p>", int level = 2) =>
        WithBlock(new HeadingBlock { Id = blockId, Style = DefaultStyle, Props = new HeadingProps(html, level, HeadingSize.Lg) });

    public SectionBuilder WithText(string blockId, string html = "<p>Text</p>") =>
        WithBlock(new TextBlock { Id = blockId, Style = DefaultStyle, Props = new TextProps(html, TextSize.Md, TextTone.Default) });

    public SectionBuilder WithButton(string blockId, string label = "Button", string href = "/") =>
        WithBlock(new ButtonBlock { Id = blockId, Style = DefaultStyle, Props = new ButtonProps(label, href, ButtonVariant.Primary, true, ButtonSize.Md) });

    public SectionBuilder WithImage(string blockId, string src = "/media/hero.svg", string alt = "An image") =>
        WithBlock(new ImageBlock { Id = blockId, Style = DefaultStyle, Props = new ImageProps(src, alt, ImageAspect.Auto, 0, "") });

    public SectionBuilder WithGallery(string blockId, params GalleryImage[] images) =>
        WithBlock(new GalleryBlock { Id = blockId, Style = DefaultStyle, Props = new GalleryProps(images, 3, 12, 8) });

    public SectionBuilder WithVideo(string blockId, string url = "https://youtu.be/dQw4w9WgXcQ") =>
        WithBlock(new VideoBlock { Id = blockId, Style = DefaultStyle, Props = new VideoProps(url, VideoAspect.Wide16x9, 0) });

    public Section Build() => new() { Id = id, Settings = _settings, Blocks = [.. _blocks] };
}
