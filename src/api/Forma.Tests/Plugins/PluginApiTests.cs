using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Forma.Application.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using static Forma.Tests.Plugins.TestPlugins;

namespace Forma.Tests.Plugins;

/// <summary>The API with a fresh database and the plugins <see cref="InstallPlugins"/> puts in its plugins folder.</summary>
public abstract class PluginApiFactory : WebApplicationFactory<Program>
{
    private readonly string _directory = Directory.CreateTempSubdirectory("forma-plugin-tests-").FullName;

    protected abstract void InstallPlugins(string root);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var plugins = Directory.CreateDirectory(Path.Combine(_directory, "plugins")).FullName;
        InstallPlugins(plugins);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Forma", $"Data Source={Path.Combine(_directory, "forma.db")}");
        builder.UseSetting("Assets:Path", Path.Combine(_directory, "assets"));
        builder.UseSetting("Plugins:Path", plugins);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { /* best effort */ }
    }
}

/// <summary>Plugins that add warnings, crash, and record notifications.</summary>
public sealed class WarningPluginsFactory : PluginApiFactory
{
    protected override void InstallPlugins(string root)
    {
        Install(root, "policy", Issues(
            new JsonObject { ["severity"] = "warning", ["code"] = "intro.tone", ["message"] = "Keep the intro friendly.", ["blockId"] = "b_intro", ["field"] = "html" },
            new JsonObject { ["severity"] = "warning", ["code"] = "ghost", ["message"] = "Points at nothing.", ["blockId"] = "b_ghost", ["field"] = "href" },
            new JsonObject { ["severity"] = "fatal", ["code"] = "bad.severity", ["message"] = "Dropped." },
            new JsonObject { ["severity"] = "warning", ["code"] = "Not A Code", ["message"] = "Dropped." }),
            PluginEvents.PageCheck);
        Install(root, "crashy", Crashes, PluginEvents.PageCheck);
        Install(root, "recorder", Echo, PluginEvents.PageDraftSaved, PluginEvents.PagePublished);
        Directory.CreateDirectory(Path.Combine(root, "broken"));
    }
}

/// <summary>A plugin that turns every page into an error, so nothing can be published.</summary>
public sealed class BlockingPluginFactory : PluginApiFactory
{
    protected override void InstallPlugins(string root) =>
        Install(root, "gatekeeper", Issues(new JsonObject { ["severity"] = "error", ["code"] = "legal.missing", ["message"] = "Add the legal notice." }),
            PluginEvents.PageCheck);
}

public class PluginApiTests(WarningPluginsFactory factory) : IClassFixture<WarningPluginsFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> PageAsync(HttpClient client, string slug)
    {
        var websites = await client.GetFromJsonAsync<JsonArray>("/websites");
        var pages = await client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websites![0]!["id"]}");
        return pages!.First(p => p!["slug"]!.GetValue<string>() == slug)!["id"]!.GetValue<Guid>();
    }

    [Fact]
    public async Task Plugins_are_listed_with_load_errors()
    {
        var plugins = (await _client.GetFromJsonAsync<JsonArray>("/plugins"))!.ToDictionary(p => p!["id"]!.GetValue<string>());

        Assert.Equal(["broken", "crashy", "policy", "recorder"], plugins.Keys.Order());
        Assert.True(plugins["policy"]!["loaded"]!.GetValue<bool>());
        Assert.Equal(["page.check"], plugins["policy"]!["events"]!.AsArray().Select(e => e!.GetValue<string>()));
        Assert.False(plugins["broken"]!["loaded"]!.GetValue<bool>());
        Assert.Contains("plugin.json", plugins["broken"]!["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task Check_hook_issues_are_added_prefixed_and_pointed_at_the_right_block()
    {
        var contact = await PageAsync(_client, "contact");

        var check = await (await _client.PostAsync($"/pages/{contact}/check", null)).Content.ReadFromJsonAsync<JsonObject>();
        var issues = check!["issues"]!.AsArray().Select(i => i!.AsObject()).ToList();

        var tone = Assert.Single(issues, i => i["code"]!.GetValue<string>() == "policy:intro.tone");
        Assert.Equal("warning", tone["severity"]!.GetValue<string>());
        // The plugin only named the block; the section is filled in.
        Assert.Equal("s_intro", tone["sectionId"]!.GetValue<string>());
        Assert.Equal("b_intro", tone["blockId"]!.GetValue<string>());
        Assert.Equal("html", tone["field"]!.GetValue<string>());

        // A pointer to a block that isn't on the page is dropped, with the field it was about.
        var ghost = Assert.Single(issues, i => i["code"]!.GetValue<string>() == "policy:ghost");
        Assert.Null(ghost["blockId"]);
        Assert.Null(ghost["field"]);

        // Unknown severities and malformed codes are dropped.
        Assert.Equal(2, issues.Count(i => i["code"]!.GetValue<string>().StartsWith("policy:")));

        // The plugin that crashed adds a warning instead of breaking the check.
        var failed = Assert.Single(issues, i => i["code"]!.GetValue<string>() == "plugin.failed");
        Assert.Contains("Test crashy", failed["message"]!.GetValue<string>());

        var runs = await _client.GetFromJsonAsync<JsonArray>("/plugins/crashy/runs");
        Assert.Contains(runs!, r => r!["event"]!.GetValue<string>() == "page.check" && !r["succeeded"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Saving_and_publishing_notify_subscribed_plugins_in_the_background()
    {
        var about = await PageAsync(_client, "about");
        var content = await _client.GetFromJsonAsync<JsonObject>($"/pages/{about}/content");
        var draft = content!["draft"]!.DeepClone();
        draft["sections"]!.AsArray().RemoveAt(0);
        var saved = await _client.PutAsJsonAsync($"/pages/{about}/draft",
            new JsonObject { ["document"] = draft, ["baseRevision"] = content["draftRevision"]!.GetValue<int>() });
        var revision = (await saved.Content.ReadFromJsonAsync<JsonObject>())!["revision"]!.GetValue<int>();

        var published = await _client.PostAsJsonAsync($"/pages/{about}/publish", new JsonObject { ["revision"] = revision });
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);

        // The recorder logs the event it received; wait for the background dispatcher to get to both.
        JsonArray runs = [];
        for (var attempt = 0; attempt < 50 && runs.Count < 2; attempt++)
        {
            await Task.Delay(100);
            runs = (await _client.GetFromJsonAsync<JsonArray>("/plugins/recorder/runs"))!;
        }

        Assert.Equal(["page.published", "page.draftSaved"], runs.Select(r => r!["event"]!.GetValue<string>()));
        Assert.All(runs, r => Assert.True(r!["succeeded"]!.GetValue<bool>()));

        // Long events are cut in the log; the start is enough to see what the plugin got.
        var logged = runs[0]!["logs"]![0]!["message"]!.GetValue<string>();
        Assert.StartsWith("""{"event":"page.published","id":""", logged);
        Assert.Contains("\"slug\":\"about\"", logged);
    }

    [Fact]
    public async Task Runs_of_an_unknown_plugin_are_404()
    {
        var response = await _client.GetAsync("/plugins/nope/runs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class BlockingPluginApiTests(BlockingPluginFactory factory) : IClassFixture<BlockingPluginFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_plugin_error_blocks_publishing()
    {
        var websites = await _client.GetFromJsonAsync<JsonArray>("/websites");
        var pages = await _client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websites![0]!["id"]}");
        var about = pages!.First(p => p!["slug"]!.GetValue<string>() == "about")!["id"]!.GetValue<Guid>();
        var content = await _client.GetFromJsonAsync<JsonObject>($"/pages/{about}/content");

        var response = await _client.PostAsJsonAsync($"/pages/{about}/publish", new JsonObject { ["revision"] = content!["draftRevision"]!.GetValue<int>() });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        var issue = body!["issues"]![0]!;
        Assert.Equal("gatekeeper:legal.missing", issue["code"]!.GetValue<string>());
        Assert.Equal("error", issue["severity"]!.GetValue<string>());
    }
}
