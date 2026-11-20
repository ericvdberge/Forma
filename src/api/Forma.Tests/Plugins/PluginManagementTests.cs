using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Forma.Application.Plugins;
using Forma.Infrastructure.Plugins;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Forma.Tests.Plugins.TestPlugins;

namespace Forma.Tests.Plugins;

/// <summary>Turning plugins off and on, and deleting them, while the API runs.</summary>
public sealed class PluginCatalogManagementTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("forma-plugin-manage-").FullName;
    private readonly WasmSandbox _sandbox = new(Options.Create(new PluginOptions()));

    private PluginCatalog NewCatalog() =>
        new(_sandbox, Options.Create(new PluginOptions { Path = _root }), PluginEvents.All, NullLogger<PluginCatalog>.Instance);

    [Fact]
    public void Turning_a_plugin_off_is_immediate_and_survives_a_restart()
    {
        Install(_root, "alpha", Echo, PluginEvents.PageCheck);
        Install(_root, "beta", Echo, PluginEvents.PageCheck);
        var catalog = NewCatalog();

        Assert.Equal(PluginChange.Done, catalog.SetEnabled("alpha", false));

        var alpha = catalog.Plugins.Single(p => p.Id == "alpha");
        Assert.False(alpha.Enabled);
        Assert.False(alpha.IsActive);
        Assert.True(alpha.IsLoaded);
        Assert.True(catalog.Plugins.Single(p => p.Id == "beta").IsActive);

        // A new catalog (an API restart) reads plugins.state.json.
        Assert.False(NewCatalog().Plugins.Single(p => p.Id == "alpha").Enabled);

        Assert.Equal(PluginChange.Done, catalog.SetEnabled("alpha", true));
        Assert.True(NewCatalog().Plugins.Single(p => p.Id == "alpha").IsActive);
    }

    [Fact]
    public void Deleting_a_plugin_removes_its_folder_and_state()
    {
        Install(_root, "alpha", Echo, PluginEvents.PageCheck);
        Directory.CreateDirectory(Path.Combine(_root, "broken"));
        var catalog = NewCatalog();
        catalog.SetEnabled("alpha", false);

        Assert.Equal(PluginChange.Done, catalog.Delete("alpha"));
        // A plugin that didn't load can be deleted too; its id is its folder.
        Assert.Equal(PluginChange.Done, catalog.Delete("broken"));

        Assert.Empty(catalog.Plugins);
        Assert.False(Directory.Exists(Path.Combine(_root, "alpha")));
        Assert.DoesNotContain("alpha", File.ReadAllText(Path.Combine(_root, PluginCatalog.StateFileName)));
        Assert.Empty(NewCatalog().Plugins);
    }

    [Fact]
    public void Unknown_plugins_are_not_found()
    {
        var catalog = NewCatalog();

        Assert.Equal(PluginChange.NotFound, catalog.SetEnabled("nope", false));
        Assert.Equal(PluginChange.NotFound, catalog.Delete("nope"));
    }

    [Fact]
    public void A_damaged_state_file_turns_every_plugin_on()
    {
        Install(_root, "alpha", Echo, PluginEvents.PageCheck);
        File.WriteAllText(Path.Combine(_root, PluginCatalog.StateFileName), "{ not json");

        Assert.True(Assert.Single(NewCatalog().Plugins).IsActive);
    }

    public void Dispose()
    {
        _sandbox.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
    }
}

/// <summary>A check plugin that always warns, and a plugin with a block, to turn off and delete.</summary>
public sealed class ManagedPluginsFactory : PluginApiFactory
{
    protected override void InstallPlugins(string root)
    {
        Install(root, "nagger", Issues(new JsonObject { ["severity"] = "warning", ["code"] = "nag", ["message"] = "Nag." }), PluginEvents.PageCheck);
        PluginBlockManifests.InstallBlockPlugin(root, "cards");
    }
}

public class PluginManagementApiTests(ManagedPluginsFactory factory) : IClassFixture<ManagedPluginsFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<List<string>> CheckCodesAsync()
    {
        var websites = await _client.GetFromJsonAsync<JsonArray>("/websites");
        var pages = await _client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websites![0]!["id"]}");
        var page = pages![0]!["id"]!.GetValue<Guid>();
        var check = await (await _client.PostAsync($"/pages/{page}/check", null)).Content.ReadFromJsonAsync<JsonObject>();
        return check!["issues"]!.AsArray().Select(i => i!["code"]!.GetValue<string>()).ToList();
    }

    [Fact]
    public async Task A_plugin_turned_off_stops_running_and_offering_blocks_until_turned_on()
    {
        Assert.Contains("nagger:nag", await CheckCodesAsync());

        var off = await _client.PutAsJsonAsync("/plugins/nagger/enabled", new { enabled = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False((await off.Content.ReadFromJsonAsync<JsonObject>())!["enabled"]!.GetValue<bool>());
        Assert.DoesNotContain("nagger:nag", await CheckCodesAsync());

        var cardsOff = await (await _client.PutAsJsonAsync("/plugins/cards/enabled", new { enabled = false })).Content.ReadFromJsonAsync<JsonObject>();
        Assert.Empty(cardsOff!["blocks"]!.AsArray());

        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync("/plugins/nagger/enabled", new { enabled = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync("/plugins/cards/enabled", new { enabled = true })).StatusCode);
        Assert.Contains("nagger:nag", await CheckCodesAsync());
    }

    [Fact]
    public async Task Unknown_plugins_are_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync("/plugins/nope/enabled", new { enabled = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/plugins/nope")).StatusCode);
    }
}

/// <summary>Its own API, since deleting changes what the other tests see.</summary>
public sealed class DeletablePluginFactory : PluginApiFactory
{
    protected override void InstallPlugins(string root) =>
        Install(root, "doomed", Issues(new JsonObject { ["severity"] = "warning", ["code"] = "nag", ["message"] = "Nag." }), PluginEvents.PageCheck);
}

public class PluginDeleteApiTests(DeletablePluginFactory factory) : IClassFixture<DeletablePluginFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_deleted_plugin_is_gone_from_the_list_and_its_runs()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync("/plugins/doomed")).StatusCode);

        Assert.Empty((await _client.GetFromJsonAsync<JsonArray>("/plugins"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/plugins/doomed/runs")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/plugins/doomed")).StatusCode);
    }
}
