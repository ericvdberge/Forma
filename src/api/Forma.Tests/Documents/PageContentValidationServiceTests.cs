using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Tests.Builders;

namespace Forma.Tests.Documents;

public class PageContentValidationServiceTests
{
    private static readonly Guid WebsiteId = Guid.NewGuid();
    private static readonly Guid KnownMedia = Guid.NewGuid();

    private readonly PageContentValidationService _service = new(new FakeMedia(), new FakePages());

    private Task<IReadOnlyList<ContentIssue>> ValidateAsync(PageDocument document) =>
        _service.ValidateAsync(document, WebsiteId);

    [Theory]
    [InlineData("", "link.empty", IssueSeverity.Error)]
    [InlineData("#", "link.placeholder", IssueSeverity.Warning)]
    [InlineData("/pricing", "link.page_missing", IssueSeverity.Warning)]
    public async Task Flags_buttons_that_go_nowhere(string href, string code, IssueSeverity severity)
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithButton("b_cta", "Go", href))
            .Build();

        var issues = await ValidateAsync(document);

        var issue = Assert.Single(issues);
        Assert.Equal((code, severity, "b_cta", "href"), (issue.Code, issue.Severity, issue.BlockId, issue.Field));
    }

    [Theory]
    [InlineData("/about")]
    [InlineData("/about#team")]
    [InlineData("/")]
    [InlineData("https://example.com")]
    [InlineData("mailto:hi@example.com")]
    public async Task Accepts_real_links(string href)
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithButton("b_cta", "Go", href))
            .Build();

        Assert.Empty(await ValidateAsync(document));
    }

    [Fact]
    public async Task Points_at_the_gallery_item_with_a_deleted_image()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithGallery(
                "b_gallery",
                new GalleryImage($"/media/{KnownMedia}/image", "a"),
                new GalleryImage($"/media/{Guid.NewGuid()}/image", "b"),
                new GalleryImage("/media/hero.svg", "c")))
            .Build();

        var issues = await ValidateAsync(document);

        var issue = Assert.Single(issues);
        Assert.Equal(("media.not_found", "images", 1, "s_a.b_gallery.images[1]"), (issue.Code, issue.Field, issue.ItemIndex, issue.Location));
    }

    [Fact]
    public async Task Warns_about_heading_order()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section
                .WithHeading("b_a", level: 1)
                .WithHeading("b_b", level: 3)
                .WithHeading("b_c", level: 1))
            .Build();

        var issues = await ValidateAsync(document);

        Assert.Equal(["heading.skipped_level", "heading.multiple_h1"], issues.Select(i => i.Code));
        Assert.All(issues, i => Assert.Equal(IssueSeverity.Warning, i.Severity));
    }

    [Fact]
    public async Task Errors_come_before_warnings()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section
                .WithImage("b_image", alt: "")
                .WithVideo("b_video", url: ""))
            .Build();

        var issues = await ValidateAsync(document);

        Assert.Equal(["video.missing", "image.alt_missing"], issues.Select(i => i.Code));
    }

    [Fact]
    public async Task Warns_about_an_empty_page()
    {
        var issue = Assert.Single(await ValidateAsync(new PageDocumentBuilder().Build()));

        Assert.Equal(("page.empty", "document"), (issue.Code, issue.Location));
    }

    private sealed class FakeMedia : IMediaCoreService
    {
        public Task<IReadOnlyList<MediaItem>> GetMediaItemsAsync(Guid websiteId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaItem>>(websiteId == WebsiteId
                ? [new MediaItem { Id = KnownMedia, WebsiteId = WebsiteId, FileName = "a.png", ContentType = "image/png", ImagePath = "", ThumbnailPath = "" }]
                : []);

        public Task<MediaItem?> GetMediaItemAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(Stream Content, string ContentType)?> GetImageAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(Stream Content, string ContentType)?> GetThumbnailAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MediaUploadResult> UploadAsync(Guid websiteId, string fileName, Stream content, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakePages : IPageCoreService
    {
        public Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Page>>([new Page { WebsiteId = WebsiteId, Title = "About", Slug = "about" }]);

        public Task<Page?> GetPageAsync(PageFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
