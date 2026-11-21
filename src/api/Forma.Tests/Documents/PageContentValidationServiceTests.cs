using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.CollectionItems;
using Forma.Infrastructure.Repositories.Collections;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Tests.Builders;

namespace Forma.Tests.Documents;

public class PageContentValidationServiceTests
{
    private static readonly Guid WebsiteId = Guid.NewGuid();
    private static readonly Guid KnownMedia = Guid.NewGuid();

    private static readonly Collection Blog = new()
    {
        Id = Guid.NewGuid(),
        WebsiteId = WebsiteId,
        Name = "Blog posts",
        SingularName = "Blog post",
        Slug = "blog",
        Icon = "newspaper",
        Fields =
        [
            new CollectionField("fld_title", "Title", CollectionFieldType.Text, Required: true),
            new CollectionField("fld_cover", "Cover", CollectionFieldType.Image, Required: false),
            new CollectionField("fld_body", "Body", CollectionFieldType.RichText, Required: true),
            new CollectionField("fld_video", "Video", CollectionFieldType.Url, Required: true),
        ],
    };

    /// <summary>The published posts of <see cref="Blog"/>; drafts aren't returned for publish checks.</summary>
    private static readonly CollectionItem[] PublishedPosts =
    [
        Post("opening-our-studio", video: "https://youtu.be/dQw4w9WgXcQ"),
        Post("our-new-website", video: "https://acme.example"),
    ];

    private static readonly Page StaticPage = new() { WebsiteId = WebsiteId, Title = "About", Slug = "about" };

    private static readonly Page BlogDetailPage = new()
    {
        WebsiteId = WebsiteId,
        Title = "Blog post page",
        Slug = "blog/{item}",
        Kind = PageKind.CollectionTemplate,
        CollectionId = Blog.Id,
    };

    private readonly PageContentValidationService _service = new(new FakeMedia(), new FakePages(), new FakeCollections(), new FakeItems());

    private Task<IReadOnlyList<ContentIssue>> ValidateAsync(PageDocument document, Page? page = null) =>
        _service.ValidateAsync(document, page ?? StaticPage);

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
    [InlineData("/blog/opening-our-studio")]
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

    [Theory]
    [InlineData("/blog/opening-our-stuido")]
    [InlineData("/blog/a-draft-post")]
    [InlineData("/team/opening-our-studio")]
    public async Task Warns_about_links_to_items_that_arent_published(string href)
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithButton("b_cta", "Go", href))
            .Build();

        var issue = Assert.Single(await ValidateAsync(document));

        Assert.Equal(("link.page_missing", "s_a.b_cta.href"), (issue.Code, issue.Location));
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

    [Fact]
    public async Task Accepts_bindings_to_fields_of_the_detail_pages_collection()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section
                .WithHeading("b_title", level: 1, bindings: Bind("html", "fld_title"))
                .WithText("b_body", bindings: Bind("html", "fld_body")))
            .Build();

        Assert.Empty(await ValidateAsync(document, BlogDetailPage));
    }

    [Fact]
    public async Task Flags_bindings_that_cant_be_shown()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section
                .WithImage("b_title_as_image", src: "", bindings: Bind("src", "fld_title"))
                .WithHeading("b_removed", level: 1, bindings: Bind("html", "fld_gone"))
                .WithImage("b_cover", src: "", bindings: Bind("src", "fld_cover")))
            .Build();

        var issues = await ValidateAsync(document, BlogDetailPage);

        Assert.Equal(
            [
                ("binding.type_mismatch", "s_a.b_title_as_image.bindings.src"),
                ("binding.field_missing", "s_a.b_removed.bindings.html"),
                ("binding.optional_field", "s_a.b_cover.bindings.src"),
            ],
            issues.Select(i => (i.Code, i.Location)));
    }

    [Fact]
    public async Task Bindings_need_a_detail_page()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithHeading("b_title", level: 1, bindings: Bind("html", "fld_title")))
            .Build();

        var issue = Assert.Single(await ValidateAsync(document));

        Assert.Equal(("binding.page_scope", IssueSeverity.Error), (issue.Code, issue.Severity));
    }

    [Fact]
    public async Task Checks_the_collection_and_fields_of_cards()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section
                .WithCollectionCards("b_none", collectionId: null)
                .WithCollectionCards("b_deleted", collectionId: Guid.NewGuid())
                .WithCollectionCards("b_wrong_image", Blog.Id, imageField: "fld_title")
                .WithCollectionCards("b_fine", Blog.Id, imageField: "fld_cover", titleField: "fld_title", textField: "fld_body"))
            .Build();

        var issues = await ValidateAsync(document);

        Assert.Equal(
            [
                ("list.collection_missing", "s_a.b_none.collectionId"),
                ("list.collection_missing", "s_a.b_deleted.collectionId"),
                ("binding.type_mismatch", "s_a.b_wrong_image.imageField"),
            ],
            issues.Select(i => (i.Code, i.Location)));
    }

    [Fact]
    public async Task Warns_when_a_bound_video_shows_items_that_arent_videos()
    {
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithVideo("b_video", url: "", bindings: Bind("url", "fld_video")))
            .Build();

        var issue = Assert.Single(await ValidateAsync(document, BlogDetailPage));

        Assert.Equal(
            ("video.binding_not_video", IssueSeverity.Warning, "s_a.b_video.bindings.url"),
            (issue.Code, issue.Severity, issue.Location));
        Assert.Contains("1 item", issue.Message);
    }

    private static CollectionItem Post(string slug, string video) => new()
    {
        Id = Guid.NewGuid(),
        CollectionId = Blog.Id,
        Slug = slug,
        PublishedAt = DateTime.UtcNow,
        Values = new Dictionary<string, JsonElement> { ["fld_video"] = JsonSerializer.SerializeToElement(video) },
    };

    private static Dictionary<string, Binding> Bind(string prop, string fieldId) =>
        new() { [prop] = new Binding(BindingScope.Page, fieldId) };

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
        public Task<Page> CreateAsync(PageInsert insert, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Page?> UpdateAsync(Guid id, PageUpdate update, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeItems : ICollectionItemCoreService
    {
        public Task<IReadOnlyList<CollectionItem>> GetCollectionItemsAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CollectionItem>>(filter.WebsiteId == WebsiteId ? PublishedPosts : []);

        public Task<CollectionItem?> GetCollectionItemAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CollectionItem> CreateAsync(CollectionItemInsert insert, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CollectionItem?> UpdateAsync(Guid id, CollectionItemUpdate update, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeCollections : ICollectionCoreService
    {
        public Task<IReadOnlyList<Collection>> GetCollectionsAsync(CollectionFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Collection>>(filter.WebsiteId == WebsiteId ? [Blog] : []);

        public Task<Collection?> GetCollectionAsync(CollectionFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Collection> CreateAsync(CollectionInsert insert, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Collection?> UpdateAsync(Guid id, CollectionUpdate update, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
