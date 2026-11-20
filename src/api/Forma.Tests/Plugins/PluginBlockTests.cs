using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Forma.Application.Plugins;
using Forma.Infrastructure.Plugins;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Forma.Tests.Plugins.TestPlugins;

namespace Forma.Tests.Plugins;

/// <summary>Blocks that plugins add to the studio: declared in <c>plugin.json</c>, stored in pages as <c>type: "plugin"</c>.</summary>
public static class PluginBlockManifests
{
    public const string CardTemplate =
        """<div style="color:{{accent}}"><img data-if="image" src="{{image}}" alt="{{name}}"><h3>{{name}}</h3><a href="{{link}}">Buy</a></div>""";

    /// <summary>A plugin with one "card" block whose template is <paramref name="template"/>.</summary>
    public static void InstallBlockPlugin(string root, string id, string template = CardTemplate, string events = "[]")
    {
        var folder = Directory.CreateDirectory(Path.Combine(root, id)).FullName;
        File.WriteAllText(Path.Combine(folder, "plugin.json"), $$"""
            {
              "id": "{{id}}", "name": "Test {{id}}", "version": "1.0.0", "events": {{events}},
              "blocks": [{
                "id": "card", "label": "Card", "icon": "tag",
                "fields": [
                  { "key": "name", "label": "Name", "kind": "text", "default": "Chair" },
                  { "key": "image", "label": "Photo", "kind": "image" },
                  { "key": "link", "label": "Link", "kind": "url", "default": "#" },
                  { "key": "accent", "label": "Colour", "kind": "color", "default": "#000000" },
                  { "key": "size", "label": "Size", "kind": "segmented", "options": [{ "value": "s", "label": "S" }] },
                  { "key": "count", "label": "Count", "kind": "slider", "min": 0, "max": 10 }
                ],
                "template": {{JsonValue.Create(template).ToJsonString()}}
              }]
            }
            """);
        File.WriteAllBytes(Path.Combine(folder, "plugin.wasm"), Compile(Echo));
    }
}

public sealed class PluginBlockCatalogTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("forma-plugin-blocks-").FullName;
    private readonly WasmSandbox _sandbox = new(Options.Create(new PluginOptions()));

    private InstalledPlugin LoadOnly() => Assert.Single(
        new PluginCatalog(_sandbox, Options.Create(new PluginOptions { Path = _root }), PluginEvents.All, NullLogger<PluginCatalog>.Instance).Plugins);

    [Fact]
    public void A_plugin_with_only_blocks_loads_without_events()
    {
        PluginBlockManifests.InstallBlockPlugin(_root, "cards");

        var plugin = LoadOnly();

        Assert.True(plugin.IsLoaded, plugin.Error);
        var block = Assert.Single(plugin.Manifest!.Blocks!);
        Assert.Equal(["name", "image", "link", "accent", "size", "count"], block.Fields.Select(f => f.Key));
    }

    [Theory]
    [InlineData("""<div><script>alert(1)</script></div>""", "<script> isn't allowed")]
    [InlineData("""<div onclick="alert(1)">x</div>""", "'onclick' isn't allowed")]
    [InlineData("""<div class="x">x</div>""", "'class' isn't allowed")]
    [InlineData("""<a href="javascript:alert(1)">x</a>""", "'href' must be a single")]
    [InlineData("""<a href="https://shop.example/{{name}}">x</a>""", "'href' must be a single")]
    [InlineData("""<img src="//evil.example/x.png">""", "'src' must be a single")]
    [InlineData("""<div href="{{link}}">x</div>""", "only allowed on <a>")]
    [InlineData("""<div style="background:url(https://evil.example)">x</div>""", "can't load anything")]
    [InlineData("""<p>{{price}}</p>""", "{{price}}, which isn't one of its fields")]
    [InlineData("""<p data-if="price">x</p>""", "data-if=\"price\" isn't one of")]
    [InlineData("""<iframe src="{{link}}"></iframe>""", "<iframe> isn't allowed")]
    [InlineData("""<svg><a href="{{link}}">x</a></svg>""", "<svg> isn't allowed")]
    public void Unsafe_templates_are_refused_at_load_with_the_reason(string template, string reason)
    {
        PluginBlockManifests.InstallBlockPlugin(_root, "cards", template);

        var plugin = LoadOnly();

        Assert.False(plugin.IsLoaded);
        Assert.Contains("Block 'card'", plugin.Error);
        Assert.Contains(reason, plugin.Error);
    }

    public void Dispose()
    {
        _sandbox.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
    }
}

public sealed class BlockPluginFactory : PluginApiFactory
{
    protected override void InstallPlugins(string root) => PluginBlockManifests.InstallBlockPlugin(root, "cards");
}

public class PluginBlockApiTests(BlockPluginFactory factory) : IClassFixture<BlockPluginFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(Guid Page, JsonObject Content)> AboutAsync()
    {
        var websites = await _client.GetFromJsonAsync<JsonArray>("/websites");
        var pages = await _client.GetFromJsonAsync<JsonArray>($"/pages?websiteId={websites![0]!["id"]}");
        var about = pages!.First(p => p!["slug"]!.GetValue<string>() == "about")!["id"]!.GetValue<Guid>();
        return (about, (await _client.GetFromJsonAsync<JsonObject>($"/pages/{about}/content"))!);
    }

    private static JsonObject PluginBlock(string id, string plugin, JsonObject values) => new()
    {
        ["id"] = id,
        ["type"] = "plugin",
        ["props"] = new JsonObject { ["plugin"] = plugin, ["block"] = "card", ["values"] = values },
        ["style"] = new JsonObject
        {
            ["align"] = "inherit", ["marginTop"] = 0, ["marginBottom"] = 0, ["maxWidth"] = "none", ["anchor"] = "", ["className"] = "",
            ["visibility"] = new JsonObject { ["desktop"] = true, ["tablet"] = true, ["mobile"] = true },
        },
    };

    private async Task<HttpResponseMessage> SaveWithBlocksAsync(params JsonObject[] blocks)
    {
        var (about, content) = await AboutAsync();
        var draft = content["draft"]!.DeepClone();
        var section = draft["sections"]![1]!["blocks"]!.AsArray();
        foreach (var block in blocks)
        {
            section.Add(block);
        }

        return await _client.PutAsJsonAsync($"/pages/{about}/draft",
            new JsonObject { ["document"] = draft, ["baseRevision"] = content["draftRevision"]!.GetValue<int>() });
    }

    [Fact]
    public async Task Plugins_list_their_blocks_with_fields_and_template()
    {
        var plugins = await _client.GetFromJsonAsync<JsonArray>("/plugins");

        var block = Assert.Single(plugins!.Single(p => p!["id"]!.GetValue<string>() == "cards")!["blocks"]!.AsArray())!;
        Assert.Equal("Card", block["label"]!.GetValue<string>());
        Assert.Equal(PluginBlockManifests.CardTemplate, block["template"]!.GetValue<string>());
        Assert.Equal("Chair", block["fields"]![0]!["default"]!.GetValue<string>());
        Assert.Equal("s", block["fields"]![4]!["options"]![0]!["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_page_with_plugin_blocks_saves_and_warns_only_about_missing_plugins()
    {
        var saved = await SaveWithBlocksAsync(
            PluginBlock("b_card", "cards", new JsonObject { ["name"] = "Lamp", ["count"] = 3, ["sale"] = true }),
            PluginBlock("b_gone", "uninstalled", new JsonObject { ["name"] = "Old" }));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var (about, _) = await AboutAsync();
        var check = await (await _client.PostAsync($"/pages/{about}/check", null)).Content.ReadFromJsonAsync<JsonObject>();
        var missing = Assert.Single(check!["issues"]!.AsArray(), i => i!["code"]!.GetValue<string>() == "plugin.block_missing")!;

        Assert.Equal("b_gone", missing["blockId"]!.GetValue<string>());
        Assert.Contains("uninstalled", missing["message"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("""{ "nested": { "a": 1 } }""")]
    [InlineData("""{ "list": [1, 2] }""")]
    [InlineData("""{ "bad key": "x" }""")]
    public async Task Plugin_block_values_are_plain_strings_numbers_and_booleans(string values)
    {
        var saved = await SaveWithBlocksAsync(PluginBlock("b_card", "cards", JsonNode.Parse(values)!.AsObject()));

        Assert.Equal(HttpStatusCode.BadRequest, saved.StatusCode);
    }
}
