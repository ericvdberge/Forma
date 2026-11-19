using Forma.Application.Plugins;
using Forma.Infrastructure.Plugins;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Forma.Tests.Plugins.TestPlugins;

namespace Forma.Tests.Plugins;

public sealed class PluginCatalogTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("forma-plugins-").FullName;
    private readonly WasmSandbox _sandbox = new(Options.Create(new PluginOptions()));

    private IReadOnlyList<InstalledPlugin> Load() =>
        new PluginCatalog(_sandbox, Options.Create(new PluginOptions { Path = _root }), PluginEvents.All, NullLogger<PluginCatalog>.Instance).Plugins;

    private void WriteManifest(string folder, string json)
    {
        Directory.CreateDirectory(Path.Combine(_root, folder));
        File.WriteAllText(Path.Combine(_root, folder, "plugin.json"), json);
    }

    [Fact]
    public void Valid_plugins_load_and_broken_ones_are_listed_with_the_reason()
    {
        Install(_root, "good", Echo, PluginEvents.PagePublished);
        WriteManifest("mismatch", """{"id":"other","name":"X","version":"1","events":["page.published"]}""");
        WriteManifest("typo", """{"id":"typo","name":"X","version":"1","events":["page.publish"]}""");
        WriteManifest("no-module", """{"id":"no-module","name":"X","version":"1","events":["page.check"]}""");
        WriteManifest("escape", """{"id":"escape","name":"X","version":"1","events":["page.check"],"module":"../good/plugin.wasm"}""");
        WriteManifest("extra", """{"id":"extra","name":"X","version":"1","events":["page.check"],"permissions":["net"]}""");
        Directory.CreateDirectory(Path.Combine(_root, "empty"));

        var plugins = Load().ToDictionary(p => p.Folder);

        Assert.True(plugins["good"].IsLoaded, plugins["good"].Error);
        Assert.Equal(64, plugins["good"].ModuleSha256!.Length);
        Assert.Contains("folder is 'mismatch'", plugins["mismatch"].Error);
        Assert.Contains("Unknown event 'page.publish'", plugins["typo"].Error);
        Assert.Contains("doesn't exist", plugins["no-module"].Error);
        Assert.Contains("inside the plugin's folder", plugins["escape"].Error);
        Assert.Contains("plugin.json is invalid", plugins["extra"].Error);
        Assert.Contains("no plugin.json", plugins["empty"].Error);
        Assert.All(plugins.Values.Where(p => p.Folder != "good"), p => Assert.False(p.IsLoaded));
    }

    [Fact]
    public void A_missing_plugins_folder_means_no_plugins()
    {
        Directory.Delete(_root);

        Assert.Empty(Load());
    }

    public void Dispose()
    {
        _sandbox.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
    }
}
