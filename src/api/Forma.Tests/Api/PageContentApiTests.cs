using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Forma.Tests.Api;

/// <summary>The API with a fresh SQLite database (sample data seeded), shared by the tests of one class.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _directory = Directory.CreateTempSubdirectory("forma-tests-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Forma", $"Data Source={Path.Combine(_directory, "forma.db")}");
        builder.UseSetting("Assets:Path", Path.Combine(_directory, "assets"));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { /* best effort */ }
    }
}

public class PageContentApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(Guid Home, Guid Contact, Guid About)> SamplePagesAsync()
    {
        var websites = await _client.GetFromJsonAsync<JsonArray>("/websites");
        var websiteId = websites![0]!["id"]!.GetValue<Guid>();
        var pages = await _client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websiteId}");
        Guid Id(string slug) => pages!.First(p => p!["slug"]!.GetValue<string>() == slug)!["id"]!.GetValue<Guid>();
        return (Id("home"), Id("contact"), Id("about"));
    }

    private Task<HttpResponseMessage> PutDraftAsync(Guid pageId, JsonNode document, int baseRevision) =>
        _client.PutAsJsonAsync($"/pages/{pageId}/draft", new JsonObject { ["document"] = document.DeepClone(), ["baseRevision"] = baseRevision });

    [Fact]
    public async Task Sample_pages_have_seeded_content()
    {
        // About (published) and Contact (draft) are never edited by the other tests in this class.
        var (_, contact, about) = await SamplePagesAsync();

        var aboutContent = await _client.GetFromJsonAsync<JsonObject>($"/pages/{about}/content");
        var contactContent = await _client.GetFromJsonAsync<JsonObject>($"/pages/{contact}/content");

        Assert.NotEmpty(aboutContent!["draft"]!["sections"]!.AsArray());
        Assert.NotNull(aboutContent["published"]);
        Assert.False(aboutContent["hasUnpublishedChanges"]!.GetValue<bool>());
        // Contact is a draft page: content, but never published.
        Assert.Null(contactContent!["published"]);
        Assert.True(contactContent["hasUnpublishedChanges"]!.GetValue<bool>());
        Assert.DoesNotContain("{{title}}", contactContent.ToJsonString());
    }

    [Fact]
    public async Task Unknown_page_is_404()
    {
        var response = await _client.GetAsync($"/pages/{Guid.NewGuid()}/content");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Saving_increments_the_revision_and_detects_conflicts()
    {
        var (home, _, _) = await SamplePagesAsync();
        var content = await _client.GetFromJsonAsync<JsonObject>($"/pages/{home}/content");
        var revision = content!["draftRevision"]!.GetValue<int>();
        var draft = content["draft"]!.AsObject();
        draft["sections"]!.AsArray().RemoveAt(0);

        var saved = await PutDraftAsync(home, draft, revision);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var body = await saved.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(revision + 1, body!["revision"]!.GetValue<int>());
        Assert.True(body["hasUnpublishedChanges"]!.GetValue<bool>());

        // Retrying the same save is harmless: same revision, nothing written.
        var retry = await PutDraftAsync(home, draft, revision + 1);
        Assert.Equal(revision + 1, (await retry.Content.ReadFromJsonAsync<JsonObject>())!["revision"]!.GetValue<int>());

        // A save based on the old revision was made elsewhere: 409 with the current content.
        var stale = await PutDraftAsync(home, content["draft"]!, revision);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var current = await stale.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(revision + 1, current!["draftRevision"]!.GetValue<int>());

        var reloaded = await _client.GetFromJsonAsync<JsonObject>($"/pages/{home}/content");
        Assert.True(JsonNode.DeepEquals(draft, reloaded!["draft"]));
    }

    [Fact]
    public async Task Malformed_documents_get_a_validation_problem_with_the_json_path()
    {
        var (_, contact, _) = await SamplePagesAsync();
        var content = await _client.GetFromJsonAsync<JsonObject>($"/pages/{contact}/content");
        var draft = content!["draft"]!.AsObject();
        draft["sections"]![0]!["blocks"]![0]!["type"] = "marquee";

        var response = await PutDraftAsync(contact, draft, content["draftRevision"]!.GetValue<int>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        var key = Assert.Single(problem!["errors"]!.AsObject()).Key;
        Assert.StartsWith("document.sections[0].blocks[0]", key);
    }

    [Fact]
    public async Task Rule_violations_are_keyed_by_the_json_path()
    {
        var (_, contact, _) = await SamplePagesAsync();
        var content = await _client.GetFromJsonAsync<JsonObject>($"/pages/{contact}/content");
        var draft = content!["draft"]!.AsObject();
        draft["sections"]![0]!["settings"]!["splitRatio"] = 99;

        var response = await PutDraftAsync(contact, draft, content["draftRevision"]!.GetValue<int>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Contains("document.sections[0].settings.splitRatio", problem!["errors"]!.AsObject().Select(e => e.Key));
    }
}
