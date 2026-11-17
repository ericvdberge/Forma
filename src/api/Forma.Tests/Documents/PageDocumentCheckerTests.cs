using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Documents.Checks;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Repositories.Media;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Tests.Documents;

public class PageDocumentCheckerTests
{
    private static readonly Guid WebsiteId = Guid.NewGuid();
    private static readonly Guid KnownMedia = Guid.NewGuid();

    private readonly PageDocumentChecker _checker = new(new FakeMedia(), new FakePages());

    private static PageDocument Blocks() =>
        JsonSerializer.Deserialize<PageDocument>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "Studio", "blocks.json")),
            PageDocumentJson.Options)!;

    private static PageDocument With(params Block[] blocks)
    {
        var section = Blocks().Sections[0];
        return new PageDocument { Version = 1, Sections = [section with { Blocks = blocks }] };
    }

    private static T Block<T>() where T : Block => Blocks().Sections[0].Blocks.OfType<T>().First();

    private async Task<IReadOnlyList<ContentIssue>> CheckAsync(PageDocument document) =>
        await _checker.CheckAsync(document, new CheckContext(WebsiteId, Guid.NewGuid()));

    [Theory]
    [InlineData("", "link.empty", IssueSeverity.Error)]
    [InlineData("#", "link.placeholder", IssueSeverity.Warning)]
    [InlineData("/pricing", "link.page_missing", IssueSeverity.Warning)]
    public async Task Flags_buttons_that_go_nowhere(string href, string code, IssueSeverity severity)
    {
        var button = Block<ButtonBlock>();

        var issues = await CheckAsync(With(button with { Props = button.Props with { Href = href } }));

        var issue = Assert.Single(issues);
        Assert.Equal((code, severity, button.Id, "href"), (issue.Code, issue.Severity, issue.BlockId, issue.Field));
    }

    [Theory]
    [InlineData("/about")]
    [InlineData("/about#team")]
    [InlineData("/")]
    [InlineData("https://example.com")]
    [InlineData("mailto:hi@example.com")]
    public async Task Accepts_real_links(string href)
    {
        var button = Block<ButtonBlock>();

        Assert.Empty(await CheckAsync(With(button with { Props = button.Props with { Href = href } })));
    }

    [Fact]
    public async Task Points_at_the_gallery_item_with_a_deleted_image()
    {
        var gallery = Block<GalleryBlock>();
        GalleryImage[] images = [new($"/media/{KnownMedia}/image", "a"), new($"/media/{Guid.NewGuid()}/image", "b"), new("/media/hero.svg", "c")];

        var issues = await CheckAsync(With(gallery with { Props = gallery.Props with { Images = images } }));

        var issue = Assert.Single(issues);
        Assert.Equal(("media.not_found", "images", 1), (issue.Code, issue.Field, issue.ItemIndex));
    }

    [Fact]
    public async Task Warns_about_heading_order()
    {
        var heading = Block<HeadingBlock>();
        HeadingBlock Level(int level, string id) => heading with { Id = id, Props = heading.Props with { Level = level } };

        var issues = await CheckAsync(With(Level(1, "b_a"), Level(3, "b_b"), Level(1, "b_c")));

        Assert.Equal(["heading.skipped_level", "heading.multiple_h1"], issues.Select(i => i.Code));
        Assert.All(issues, i => Assert.Equal(IssueSeverity.Warning, i.Severity));
    }

    [Fact]
    public async Task Errors_come_before_warnings()
    {
        var image = Block<ImageBlock>();
        var video = Block<VideoBlock>();

        var issues = await CheckAsync(With(image with { Props = image.Props with { Alt = "" } }, video with { Props = video.Props with { Url = "" } }));

        Assert.Equal(["video.missing", "image.alt_missing"], issues.Select(i => i.Code));
    }

    [Fact]
    public async Task Every_default_block_but_the_video_is_publishable()
    {
        // A fresh video block has no link yet; everything else the studio adds has working defaults.
        var issues = await CheckAsync(Blocks());

        Assert.Equal(["video.missing"], issues.Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Code));
    }

    private sealed class FakeMedia : IMediaRepository
    {
        public Task<IReadOnlyList<MediaItemDao>> GetMediaItemsAsync(MediaFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaItemDao>>(filter.WebsiteId == WebsiteId
                ? [new MediaItemDao { Id = KnownMedia, WebsiteId = WebsiteId, FileName = "a.png", ContentType = "image/png", ImagePath = "", ThumbnailPath = "" }]
                : []);

        public Task<MediaItemDao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(MediaItemDao media, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakePages : IPageRepository
    {
        public Task<IReadOnlyList<PageDao>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PageDao>>([new PageDao { WebsiteId = WebsiteId, Title = "About", Slug = "about" }]);

        public Task<PageDao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
