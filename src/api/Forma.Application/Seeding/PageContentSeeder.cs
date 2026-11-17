using System.Text.Json;
using Forma.Application.Documents;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Repositories.PageDocuments;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Infrastructure.Repositories.Websites;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Application.Seeding;

/// <summary>
/// Gives the sample pages (home, about, contact) Design Studio content: the studio's sample home page for
/// <c>home</c>, its starter page for the others. Published sample pages get the same document as their published version.
/// Pages that already have content are left alone.
/// </summary>
public static class PageContentSeeder
{
    private static readonly HashSet<string> SampleSlugs = ["home", "about", "contact"];

    public static async Task SeedPageContentAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var pages = scope.ServiceProvider.GetRequiredService<IPageRepository>();
        var websites = scope.ServiceProvider.GetRequiredService<IWebsiteRepository>();
        var documents = scope.ServiceProvider.GetRequiredService<IPageDocumentRepository>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        var websiteNames = (await websites.GetAllAsync(cancellationToken)).ToDictionary(w => w.Id, w => w.Name);
        var samplePages = (await pages.GetPagesAsync(new PageFilter(), cancellationToken)).Where(p => SampleSlugs.Contains(p.Slug));

        var home = ReadResource("home.json");
        var starter = ReadResource("page.json");
        var seeded = new List<PageDocumentDao>();

        foreach (var page in samplePages)
        {
            if ((await documents.GetDocumentsAsync(page.Id, cancellationToken)).Count > 0)
            {
                continue;
            }

            var json = page.Slug == "home"
                ? home
                : starter
                    .Replace("{{title}}", Plain(page.Title))
                    .Replace("{{website}}", Plain(websiteNames.GetValueOrDefault(page.WebsiteId, "Your website")));

            // Read and write it back, so the stored JSON and hash are canonical like any saved draft.
            var document = PageDocumentSerializer.Deserialize(json, PageDocument.CurrentVersion);
            var (content, hash) = PageDocumentSerializer.Serialize(document);

            seeded.Add(Document(page.Id, PageDocumentDaoKind.Draft));
            if (page.Status == PageDaoStatus.Published)
            {
                seeded.Add(Document(page.Id, PageDocumentDaoKind.Published));
            }

            PageDocumentDao Document(Guid pageId, PageDocumentDaoKind kind) => new()
            {
                PageId = pageId,
                Kind = kind,
                SchemaVersion = document.Version,
                Content = content,
                ContentHash = hash,
                Revision = 1,
                UpdatedAt = now,
            };
        }

        if (seeded.Count > 0)
        {
            await documents.AddAsync(seeded, cancellationToken);
        }
    }

    /// <summary>Text safe to put inside the template's JSON strings and rich text.</summary>
    private static string Plain(string text)
    {
        var encoded = JsonEncodedText.Encode(text.Replace("<", "").Replace(">", "")).ToString();
        return encoded;
    }

    private static string ReadResource(string name)
    {
        using var stream = typeof(PageContentSeeder).Assembly.GetManifestResourceStream($"Forma.Application.Seeding.{name}")
            ?? throw new InvalidOperationException($"The sample content '{name}' is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
