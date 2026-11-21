using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.PageContents;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Infrastructure.Repositories.Websites;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Application.Seeding;

/// <summary>
/// Gives the sample pages (home, about, contact) Design Studio content: the sample home page for <c>home</c>, the
/// starter page for the others. Content of published sample pages is published too. Pages that have content are left alone.
/// </summary>
public static class PageContentSeeder
{
    private static readonly HashSet<string> SampleSlugs = ["home", "about", "contact"];

    public static async Task SeedPageContentAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var pageCoreService = scope.ServiceProvider.GetRequiredService<IPageCoreService>();
        var pageContentCoreService = scope.ServiceProvider.GetRequiredService<IPageContentCoreService>();
        var websites = scope.ServiceProvider.GetRequiredService<IWebsiteRepository>();

        var websiteNames = (await websites.GetWebsitesAsync(new WebsiteFilter(), cancellationToken)).ToDictionary(w => w.Id, w => w.Name);
        var pages = await pageCoreService.GetPagesAsync(new PageFilter(), cancellationToken);

        foreach (var page in pages.Where(p => SampleSlugs.Contains(p.Slug)))
        {
            var existing = await pageContentCoreService.GetPageContentAsync(new PageContentFilter { PageId = page.Id }, cancellationToken);
            if (existing is not null)
                continue;

            var websiteName = websiteNames.GetValueOrDefault(page.WebsiteId, "Your website");
            var json = page.Slug == "home"
                ? ReadResource("home.json")
                : ReadResource("page.json").Replace("{{title}}", Plain(page.Title)).Replace("{{website}}", Plain(websiteName));

            await pageContentCoreService.CreateAsync(new PageContentInsert
            {
                PageId = page.Id,
                Document = PageDocumentSerializer.Deserialize(json, PageDocument.CurrentVersion),
                Status = page.Status == PageStatus.Published ? PageContentStatus.Published : PageContentStatus.Draft,
            }, cancellationToken);
        }
    }

    /// <summary>Text safe to put inside the template's JSON strings and rich text.</summary>
    internal static string Plain(string text) =>
        JsonEncodedText.Encode(text.Replace("<", "").Replace(">", "")).ToString();

    internal static string ReadResource(string name)
    {
        using var stream = typeof(PageContentSeeder).Assembly.GetManifestResourceStream($"Forma.Application.Seeding.{name}")
            ?? throw new InvalidOperationException($"The sample content '{name}' is not embedded.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
