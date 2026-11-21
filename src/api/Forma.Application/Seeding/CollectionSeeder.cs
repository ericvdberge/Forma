using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.Collections;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Infrastructure.Repositories.Websites;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Application.Seeding;

/// <summary>
/// Gives every sample website a <i>Blog posts</i> collection with three published posts, its (empty) detail page, which
/// the studio fills with a starter design the first time it is opened, and a published <c>/blog</c> page that lists the
/// posts as cards. Websites that already have a collection or a <c>/blog</c> page are left alone, so deleting the
/// sample blog sticks: the <c>/blog</c> page stays behind and keeps it from coming back.
/// </summary>
public static class CollectionSeeder
{
    private static readonly IReadOnlyList<CollectionField> BlogFields =
    [
        new("fld_title", "Title", CollectionFieldType.Text, Required: true),
        new("fld_cover", "Cover image", CollectionFieldType.Image, Required: true),
        new("fld_summary", "Summary", CollectionFieldType.Text, Required: false),
        new("fld_body", "Content", CollectionFieldType.RichText, Required: true),
        new("fld_date", "Published on", CollectionFieldType.Date, Required: false),
    ];

    private static readonly (string Slug, string Title, string Cover, string Summary, string Body, string Date)[] Posts =
    [
        (
            "opening-our-studio",
            "Opening our studio",
            "/media/facade.svg",
            "After two years of planning we finally have a place of our own.",
            "<p>After two years of planning we finally have a place of our own: a bright corner studio with room to sketch, build and meet.</p><p><strong>Why now</strong></p><p>Our team grew, and so did the work. A shared space makes the quiet, focused days easier and the busy ones more fun.</p>",
            "2026-09-12"
        ),
        (
            "designing-with-light",
            "Designing with light",
            "/media/arch-light.svg",
            "How daylight shapes the spaces we draw, from the first sketch on.",
            "<p>Light is the first material we think about. It decides where a room opens up, where it closes and how it feels at four in the afternoon.</p><ul><li><p>Follow the sun across the plan.</p></li><li><p>Let shadows do some of the drawing.</p></li></ul>",
            "2026-10-03"
        ),
        (
            "a-quiet-courtyard",
            "A quiet courtyard",
            "/media/courtyard.svg",
            "A small project about slowing down in the middle of the city.",
            "<p>The brief was simple: one tree, one bench and a place to slow down in the middle of the city. Getting there took a little longer.</p>",
            "2026-11-01"
        ),
    ];

    public static async Task SeedCollectionsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var collectionCoreService = scope.ServiceProvider.GetRequiredService<ICollectionCoreService>();
        var itemCoreService = scope.ServiceProvider.GetRequiredService<ICollectionItemCoreService>();
        var pageCoreService = scope.ServiceProvider.GetRequiredService<IPageCoreService>();
        var pageContentCoreService = scope.ServiceProvider.GetRequiredService<IPageContentCoreService>();
        var websites = scope.ServiceProvider.GetRequiredService<IWebsiteRepository>();

        foreach (var website in await websites.GetAllAsync(cancellationToken))
        {
            if (await IsSeededAsync(website.Id, collectionCoreService, pageCoreService, cancellationToken))
                continue;

            var blog = await collectionCoreService.CreateAsync(new CollectionInsert
            {
                WebsiteId = website.Id,
                Name = "Blog posts",
                SingularName = "Blog post",
                Slug = "blog",
                Icon = "newspaper",
                Fields = BlogFields,
            }, cancellationToken);

            await pageCoreService.CreateAsync(new PageInsert
            {
                WebsiteId = website.Id,
                Title = "Blog post page",
                Slug = "blog/{item}",
                Kind = PageKind.CollectionTemplate,
                CollectionId = blog.Id,
            }, cancellationToken);

            await SeedBlogPageAsync(website.Id, website.Name, blog.Id, pageCoreService, pageContentCoreService, cancellationToken);

            foreach (var post in Posts)
                await itemCoreService.CreateAsync(new CollectionItemInsert
                {
                    CollectionId = blog.Id,
                    Slug = post.Slug,
                    Status = CollectionItemStatus.Published,
                    Values = Values(post),
                }, cancellationToken);
        }
    }

    /// <summary>Whether the website has a collection or a <c>/blog</c> page, from an earlier run or made by hand.</summary>
    private static async Task<bool> IsSeededAsync(
        Guid websiteId,
        ICollectionCoreService collectionCoreService,
        IPageCoreService pageCoreService,
        CancellationToken cancellationToken)
    {
        var collections = await collectionCoreService.GetCollectionsAsync(new CollectionFilter { WebsiteId = websiteId }, cancellationToken);

        if (collections.Count > 0)
            return true;

        var blogPage = await pageCoreService.GetPageAsync(new PageFilter { WebsiteId = websiteId, Slug = "blog" }, cancellationToken);

        return blogPage is not null;
    }

    /// <summary>A published <c>/blog</c> page: an intro, then the posts as cards (<c>blog.json</c>).</summary>
    private static async Task SeedBlogPageAsync(
        Guid websiteId,
        string websiteName,
        Guid collectionId,
        IPageCoreService pageCoreService,
        IPageContentCoreService pageContentCoreService,
        CancellationToken cancellationToken)
    {
        var page = await pageCoreService.CreateAsync(new PageInsert
        {
            WebsiteId = websiteId,
            Title = "Blog",
            Slug = "blog",
            Status = PageStatus.Published,
        }, cancellationToken);

        var json = PageContentSeeder.ReadResource("blog.json")
            .Replace("{{collectionId}}", collectionId.ToString())
            .Replace("{{website}}", PageContentSeeder.Plain(websiteName));

        await pageContentCoreService.CreateAsync(new PageContentInsert
        {
            PageId = page.Id,
            Document = PageDocumentSerializer.Deserialize(json, PageDocument.CurrentVersion),
            Status = PageContentStatus.Published,
        }, cancellationToken);
    }

    private static IReadOnlyDictionary<string, JsonElement> Values((string Slug, string Title, string Cover, string Summary, string Body, string Date) post) =>
        new Dictionary<string, JsonElement>
        {
            ["fld_title"] = JsonSerializer.SerializeToElement(post.Title),
            ["fld_cover"] = JsonSerializer.SerializeToElement(post.Cover),
            ["fld_summary"] = JsonSerializer.SerializeToElement(post.Summary),
            ["fld_body"] = JsonSerializer.SerializeToElement(post.Body),
            ["fld_date"] = JsonSerializer.SerializeToElement(post.Date),
        };
}
