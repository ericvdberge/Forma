using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wasmtime;

namespace Forma.Infrastructure.Plugins;

/// <summary>A folder under the plugins path: a plugin that loaded, or one that didn't and why.</summary>
/// <param name="Folder">The folder's name.</param>
/// <param name="Manifest">The manifest; null when <c>plugin.json</c> is missing or unreadable.</param>
/// <param name="Module">The compiled module; null when the plugin failed to load.</param>
/// <param name="ModuleSha256">SHA-256 of the module file (lowercase hex), so people can tell builds apart.</param>
/// <param name="Error">Why it failed to load; null when it loaded.</param>
public sealed record InstalledPlugin(string Folder, PluginManifest? Manifest, Module? Module, string? ModuleSha256, string? Error)
{
    public bool IsLoaded => Module is not null;
}

/// <summary>The installed plugins, read once from <see cref="PluginOptions.Path"/>.</summary>
public interface IPluginCatalog
{
    IReadOnlyList<InstalledPlugin> Plugins { get; }
}

/// <summary>
/// Reads every <c>{Plugins:Path}/{id}/plugin.json</c> and compiles its module. A broken plugin is listed with its error
/// instead of stopping the API.
/// </summary>
public sealed partial class PluginCatalog : IPluginCatalog, IDisposable
{
    private const int MaxModuleBytes = 10 * 1024 * 1024;

    private static readonly JsonSerializerOptions ManifestJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    private readonly Lazy<IReadOnlyList<InstalledPlugin>> _plugins;

    /// <param name="knownEvents">The events plugins may subscribe to; anything else is a load error (a typo, or a newer plugin).</param>
    public PluginCatalog(IWasmSandbox sandbox, IOptions<PluginOptions> options, IReadOnlySet<string> knownEvents, ILogger<PluginCatalog> logger)
    {
        _plugins = new(() => Load(sandbox, Path.GetFullPath(options.Value.Path), knownEvents, logger));
    }

    public IReadOnlyList<InstalledPlugin> Plugins => _plugins.Value;

    private static List<InstalledPlugin> Load(IWasmSandbox sandbox, string root, IReadOnlySet<string> knownEvents, ILogger logger)
    {
        if (!Directory.Exists(root))
        {
            logger.LogInformation("No plugins folder at {Path}; no plugins are installed", root);
            return [];
        }

        var plugins = new List<InstalledPlugin>();
        foreach (var folder in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
        {
            var plugin = LoadOne(sandbox, folder, knownEvents);
            plugins.Add(plugin);
            if (plugin.IsLoaded)
            {
                logger.LogInformation("Loaded plugin {PluginId} {Version} for {Events}",
                    plugin.Manifest!.Id, plugin.Manifest.Version, string.Join(", ", plugin.Manifest.Events));
            }
            else
            {
                logger.LogWarning("Plugin in {Folder} was not loaded: {Error}", plugin.Folder, plugin.Error);
            }
        }

        return plugins;
    }

    private static InstalledPlugin LoadOne(IWasmSandbox sandbox, string folder, IReadOnlySet<string> knownEvents)
    {
        var name = Path.GetFileName(folder);
        PluginManifest? manifest = null;
        try
        {
            var manifestPath = Path.Combine(folder, "plugin.json");
            if (!File.Exists(manifestPath))
            {
                throw new PluginLoadException("There is no plugin.json.");
            }

            try
            {
                manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath), ManifestJson)
                    ?? throw new PluginLoadException("plugin.json is empty.");
            }
            catch (JsonException e)
            {
                throw new PluginLoadException($"plugin.json is invalid: {e.Message}");
            }

            Validate(manifest, name, knownEvents);

            // The module must stay inside the plugin's folder.
            var modulePath = Path.GetFullPath(Path.Combine(folder, manifest.Module));
            if (!modulePath.StartsWith(Path.GetFullPath(folder) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new PluginLoadException("'module' must be a file inside the plugin's folder.");
            }

            if (!File.Exists(modulePath))
            {
                throw new PluginLoadException($"The module file '{manifest.Module}' doesn't exist.");
            }

            if (new FileInfo(modulePath).Length > MaxModuleBytes)
            {
                throw new PluginLoadException($"The module is larger than {MaxModuleBytes / 1024 / 1024} MB.");
            }

            var bytes = File.ReadAllBytes(modulePath);
            var module = sandbox.Compile(manifest.Id, bytes);
            return new InstalledPlugin(name, manifest, module, Convert.ToHexStringLower(SHA256.HashData(bytes)), null);
        }
        catch (PluginLoadException e)
        {
            return new InstalledPlugin(name, manifest, null, null, e.Message);
        }
        catch (IOException e)
        {
            return new InstalledPlugin(name, manifest, null, null, $"Couldn't read the plugin's files: {e.Message}");
        }
    }

    private static void Validate(PluginManifest manifest, string folder, IReadOnlySet<string> knownEvents)
    {
        if (!IdPattern().IsMatch(manifest.Id))
        {
            throw new PluginLoadException("'id' must be 1–40 lowercase letters, digits and dashes, starting with a letter.");
        }

        if (manifest.Id != folder)
        {
            throw new PluginLoadException($"'id' is '{manifest.Id}' but the folder is '{folder}'; they must be the same.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 80)
        {
            throw new PluginLoadException("'name' must be 1–80 characters.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version) || manifest.Version.Length > 40)
        {
            throw new PluginLoadException("'version' must be 1–40 characters.");
        }

        if (manifest.Description is { Length: > 300 })
        {
            throw new PluginLoadException("'description' must be at most 300 characters.");
        }

        if (manifest.Events.Count == 0)
        {
            throw new PluginLoadException("'events' must list at least one event.");
        }

        foreach (var name in manifest.Events)
        {
            if (!knownEvents.Contains(name))
            {
                throw new PluginLoadException($"Unknown event '{name}'. Known events: {string.Join(", ", knownEvents.Order())}.");
            }
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{0,39}$")]
    private static partial Regex IdPattern();

    public void Dispose()
    {
        if (!_plugins.IsValueCreated)
        {
            return;
        }

        foreach (var plugin in _plugins.Value)
        {
            plugin.Module?.Dispose();
        }
    }
}
