using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Forma.Tests.Api;

public class PublishApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>A block as the studio creates it, from the contract fixtures.</summary>
    private static JsonObject StudioBlock(string type)
    {
        var blocks = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "Studio", "blocks.json")))!;
        var block = blocks["sections"]![0]!["blocks"]!.AsArray().First(b => b!["type"]!.GetValue<string>() == type)!.DeepClone().AsObject();
        block["id"] = $"b_test_{type}";
        return block;
    }

    private async Task<(Guid WebsiteId, Guid PageId)> PageAsync(string slug)
    {
        var websites = await _client.GetFromJsonAsync<JsonArray>("/websites");
        var websiteId = websites![0]!["id"]!.GetValue<Guid>();
        var pages = await _client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websiteId}");
        return (websiteId, pages!.First(p => p!["slug"]!.GetValue<string>() == slug)!["id"]!.GetValue<Guid>());
    }

    /// <summary>Saves the page's draft with <paramref name="change"/> applied to its first section's blocks; returns the new revision.</summary>
    private async Task<int> SaveAsync(Guid pageId, Action<JsonArray> change)
    {
        var content = await _client.GetFromJsonAsync<JsonObject>($"/pages/{pageId}/content");
        var draft = content!["draft"]!.DeepClone();
        change(draft["sections"]![0]!["blocks"]!.AsArray());
        var response = await _client.PutAsJsonAsync($"/pages/{pageId}/draft",
            new JsonObject { ["document"] = draft, ["baseRevision"] = content["draftRevision"]!.GetValue<int>() });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!["revision"]!.GetValue<int>();
    }

    private Task<HttpResponseMessage> PublishAsync(Guid pageId, int revision) =>
        _client.PostAsJsonAsync($"/pages/{pageId}/publish", new JsonObject { ["revision"] = revision });

    [Fact]
    public async Task Publishing_a_draft_with_errors_is_refused_with_the_issues()
    {
        var (_, pageId) = await PageAsync("contact");
        var image = StudioBlock("image");
        image["props"]!["src"] = "";
        var revision = await SaveAsync(pageId, blocks => blocks.Add(image));

        var response = await PublishAsync(pageId, revision);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var issues = (await response.Content.ReadFromJsonAsync<JsonObject>())!["issues"]!.AsArray();
        var missing = issues.Single(i => i!["code"]!.GetValue<string>() == "image.missing")!;
        Assert.Equal("error", missing["severity"]!.GetValue<string>());
        Assert.Equal("b_test_image", missing["blockId"]!.GetValue<string>());
        Assert.Equal("src", missing["field"]!.GetValue<string>());

        // Nothing went live.
        var content = await _client.GetFromJsonAsync<JsonObject>($"/pages/{pageId}/content");
        Assert.Null(content!["published"]);
    }

    [Fact]
    public async Task Publishing_copies_the_draft_and_marks_the_page_published()
    {
        var (websiteId, pageId) = await PageAsync("about");
        var badge = StudioBlock("badge");
        var revision = await SaveAsync(pageId, blocks => blocks.Add(badge));

        var response = await PublishAsync(pageId, revision);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.False(content!["hasUnpublishedChanges"]!.GetValue<bool>());
        Assert.True(JsonNode.DeepEquals(content["draft"], content["published"]));
        Assert.Contains("b_test_badge", content["published"]!.ToJsonString());

        var pages = await _client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websiteId}");
        Assert.Equal("published", pages!.First(p => p!["id"]!.GetValue<Guid>() == pageId)!["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task Publishing_an_old_revision_is_a_conflict()
    {
        var (_, pageId) = await PageAsync("home");
        var first = await SaveAsync(pageId, blocks => blocks.Add(StudioBlock("divider")));
        await SaveAsync(pageId, blocks => blocks.RemoveAt(blocks.Count - 1));

        var response = await PublishAsync(pageId, first);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(first + 1, (await response.Content.ReadFromJsonAsync<JsonObject>())!["draftRevision"]!.GetValue<int>());
    }

    [Fact]
    public async Task Images_from_another_website_or_deleted_media_are_errors()
    {
        var (_, pageId) = await PageAsync("home");
        var image = StudioBlock("image");
        image["props"]!["src"] = $"/media/{Guid.NewGuid()}/image";
        await SaveAsync(pageId, blocks => blocks.Add(image));

        var check = await (await _client.PostAsync($"/pages/{pageId}/check", null)).Content.ReadFromJsonAsync<JsonObject>();

        Assert.Contains(check!["issues"]!.AsArray(), i => i!["code"]!.GetValue<string>() == "media.not_found");
    }

    [Fact]
    public async Task Unknown_page_is_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync($"/pages/{Guid.NewGuid()}/check", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PublishAsync(Guid.NewGuid(), 1)).StatusCode);
    }
}

/// <summary>On its own database: the tests above change the sample pages.</summary>
public class SamplePagesCheckTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Sample_pages_have_no_errors()
    {
        foreach (var slug in new[] { "home", "about", "contact" })
        {
            var websites = await _client.GetFromJsonAsync<JsonArray>("/websites");
            var pages = await _client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websites![0]!["id"]}");
            var pageId = pages!.First(p => p!["slug"]!.GetValue<string>() == slug)!["id"]!.GetValue<Guid>();
            var check = await (await _client.PostAsync($"/pages/{pageId}/check", null)).Content.ReadFromJsonAsync<JsonObject>();

            var errors = check!["issues"]!.AsArray().Where(i => i!["severity"]!.GetValue<string>() == "error");
            Assert.Empty(errors);
        }
    }
}
