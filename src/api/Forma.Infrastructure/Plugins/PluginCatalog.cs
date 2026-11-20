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
    /// <summary>The plugin's id: its manifest id, or the folder name when the manifest couldn't be read.</summary>
    public string Id => Manifest?.Id ?? Folder;

    public bool IsLoaded => Module is not null;

    /// <summary>False when someone turned it off; it stays installed but isn't run and its blocks aren't offered.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Loaded and turned on: runs for its events and offers its blocks.</summary>
    public bool IsActive => IsLoaded && Enabled;
}

/// <summary>What happened when a plugin was turned on or off, or deleted.</summary>
public enum PluginChange
{
    Done,
    NotFound,

    /// <summary>The plugins folder couldn't be written, e.g. it is mounted read-only.</summary>
    NotWritable,
}

/// <summary>
/// The installed plugins, read once from <see cref="PluginOptions.Path"/>. Plugins can be turned off and on, and deleted,
/// while the API runs; adding one still needs a restart.
/// </summary>
public interface IPluginCatalog
{
    IReadOnlyList<InstalledPlugin> Plugins { get; }

    /// <summary>Turns a plugin on or off and remembers it in <c>plugins.state.json</c>, so it survives restarts.</summary>
    PluginChange SetEnabled(string pluginId, bool enabled);

    /// <summary>Deletes the plugin's folder and stops running it.</summary>
    PluginChange Delete(string pluginId);
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

    /// <summary>Which plugins are turned off, kept next to the plugins so it moves with them.</summary>
    public const string StateFileName = "plugins.state.json";

    private readonly Lazy<IReadOnlyList<InstalledPlugin>> _loaded;
    private readonly string _root;
    private readonly ILogger _logger;
    private readonly Lock _gate = new();

    /// <summary>Replaced as a whole on every change, so readers never see a half-updated list.</summary>
    private IReadOnlyList<InstalledPlugin>? _plugins;

    /// <param name="knownEvents">The events plugins may subscribe to; anything else is a load error (a typo, or a newer plugin).</param>
    public PluginCatalog(IWasmSandbox sandbox, IOptions<PluginOptions> options, IReadOnlySet<string> knownEvents, ILogger<PluginCatalog> logger)
    {
        _root = Path.GetFullPath(options.Value.Path);
        _logger = logger;
        _loaded = new(() => Load(sandbox, _root, knownEvents, logger));
    }

    public IReadOnlyList<InstalledPlugin> Plugins => Volatile.Read(ref _plugins) ?? _loaded.Value;

    public PluginChange SetEnabled(string pluginId, bool enabled) => Change(pluginId, (plugins, index) =>
    {
        var updated = plugins.ToList();
        updated[index] = plugins[index] with { Enabled = enabled };
        WriteState(updated);
        _logger.LogInformation("Plugin {PluginId} turned {State}", pluginId, enabled ? "on" : "off");
        return updated;
    });

    /// <remarks>
    /// The module isn't disposed here: a run that already started may still be using it. Nothing references it
    /// afterwards, so it is freed with the next garbage collection.
    /// </remarks>
    public PluginChange Delete(string pluginId) => Change(pluginId, (plugins, index) =>
    {
        Directory.Delete(Path.Combine(_root, plugins[index].Folder), recursive: true);
        var updated = plugins.Where((_, i) => i != index).ToList();
        WriteState(updated);
        _logger.LogInformation("Plugin {PluginId} deleted", pluginId);
        return updated;
    });

    private PluginChange Change(string pluginId, Func<IReadOnlyList<InstalledPlugin>, int, IReadOnlyList<InstalledPlugin>> change)
    {
        lock (_gate)
        {
            var plugins = Plugins;
            var index = plugins.ToList().FindIndex(p => p.Id == pluginId);
            if (index < 0)
            {
                return PluginChange.NotFound;
            }

            try
            {
                Volatile.Write(ref _plugins, change(plugins, index));
                return PluginChange.Done;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(e, "Couldn't change plugin {PluginId}: the plugins folder isn't writable", pluginId);
                return PluginChange.NotWritable;
            }
        }
    }

    private sealed record PluginState(IReadOnlyList<string> Disabled);

    private void WriteState(IReadOnlyList<InstalledPlugin> plugins)
    {
        var state = new PluginState(plugins.Where(p => !p.Enabled).Select(p => p.Id).Order(StringComparer.Ordinal).ToList());
        var path = Path.Combine(_root, StateFileName);
        // Write then rename, so a crash never leaves half a file.
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(state, ManifestJson));
        File.Move(path + ".tmp", path, overwrite: true);
    }

    private static HashSet<string> ReadDisabled(string root, ILogger logger)
    {
        var path = Path.Combine(root, StateFileName);
        try
        {
            return File.Exists(path)
                ? (JsonSerializer.Deserialize<PluginState>(File.ReadAllText(path), ManifestJson)?.Disabled ?? []).ToHashSet()
                : [];
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            logger.LogWarning(e, "Couldn't read {File}; every plugin is turned on", StateFileName);
            return [];
        }
    }

    private static List<InstalledPlugin> Load(IWasmSandbox sandbox, string root, IReadOnlySet<string> knownEvents, ILogger logger)
    {
        if (!Directory.Exists(root))
        {
            logger.LogInformation("No plugins folder at {Path}; no plugins are installed", root);
            return [];
        }

        var disabled = ReadDisabled(root, logger);
        var plugins = new List<InstalledPlugin>();
        foreach (var folder in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
        {
            var loaded = LoadOne(sandbox, folder, knownEvents);
            var plugin = disabled.Contains(loaded.Id) ? loaded with { Enabled = false } : loaded;
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

        if (manifest.Events.Count == 0 && manifest.Blocks is not { Count: > 0 })
        {
            throw new PluginLoadException("'events' must list at least one event, unless the plugin only adds 'blocks'.");
        }

        foreach (var name in manifest.Events)
        {
            if (!knownEvents.Contains(name))
            {
                throw new PluginLoadException($"Unknown event '{name}'. Known events: {string.Join(", ", knownEvents.Order())}.");
            }
        }

        ValidateBlocks(manifest.Blocks ?? []);
    }

    private const int MaxBlocks = 10;
    private const int MaxBlockFields = 20;

    private static void ValidateBlocks(IReadOnlyList<PluginBlockDefinition> blocks)
    {
        if (blocks.Count > MaxBlocks)
        {
            throw new PluginLoadException($"A plugin can add at most {MaxBlocks} blocks.");
        }

        var blockIds = new HashSet<string>();
        foreach (var block in blocks)
        {
            if (!IdPattern().IsMatch(block.Id) || !blockIds.Add(block.Id))
            {
                throw new PluginLoadException($"Block '{block.Id}': 'id' must be unique and 1–40 lowercase letters, digits and dashes.");
            }

            void Fail(string problem) => throw new PluginLoadException($"Block '{block.Id}': {problem}.");

            if (string.IsNullOrWhiteSpace(block.Label) || block.Label.Length > 40)
            {
                Fail("'label' must be 1–40 characters");
            }

            if (block.Description is { Length: > 200 } || block.Icon is { Length: > 40 })
            {
                Fail("'description' must be at most 200 characters and 'icon' at most 40");
            }

            if (block.Fields.Count > MaxBlockFields)
            {
                Fail($"a block can have at most {MaxBlockFields} fields");
            }

            var keys = new HashSet<string>();
            foreach (var field in block.Fields)
            {
                if (!FieldKeyPattern().IsMatch(field.Key) || !keys.Add(field.Key))
                {
                    Fail($"field '{field.Key}': 'key' must be unique, start with a letter and use only letters, digits and _");
                }

                if (string.IsNullOrWhiteSpace(field.Label) || field.Label.Length > 60)
                {
                    Fail($"field '{field.Key}': 'label' must be 1–60 characters");
                }

                if (!PluginBlockFieldKinds.All.Contains(field.Kind))
                {
                    Fail($"field '{field.Key}': unknown kind '{field.Kind}'. Known kinds: {string.Join(", ", PluginBlockFieldKinds.All.Order())}");
                }

                if (PluginBlockFieldKinds.WithOptions.Contains(field.Kind) && field.Options is not { Count: > 0 and <= 20 })
                {
                    Fail($"field '{field.Key}': a {field.Kind} needs 1–20 'options'");
                }

                if (PluginBlockFieldKinds.WithRange.Contains(field.Kind) && (field.Min is null || field.Max is null || field.Min >= field.Max))
                {
                    Fail($"field '{field.Key}': a {field.Kind} needs 'min' below 'max'");
                }

                if (field.Default is { } value
                    && value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
                {
                    Fail($"field '{field.Key}': 'default' must be a string, number or boolean");
                }
            }

            if (PluginBlockTemplate.Problem(block.Template, keys) is { } problem)
            {
                Fail(problem);
            }
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{0,39}$")]
    private static partial Regex IdPattern();

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]{0,39}$")]
    private static partial Regex FieldKeyPattern();

    public void Dispose()
    {
        if (!_loaded.IsValueCreated)
        {
            return;
        }

        foreach (var plugin in Plugins)
        {
            plugin.Module?.Dispose();
        }
    }
}
