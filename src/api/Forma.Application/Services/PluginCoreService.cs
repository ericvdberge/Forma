using Forma.Application.Plugins;
using Forma.Infrastructure.Plugins;

namespace Forma.Application.Services;

/// <summary>An installed plugin as people see it.</summary>
/// <param name="Id">The plugin's id (its folder name when it has no readable manifest).</param>
/// <param name="Name">The plugin's name; the folder name when it has no readable manifest.</param>
/// <param name="Version">The plugin's version, when known.</param>
/// <param name="Description">What it does, when known.</param>
/// <param name="Events">The events it subscribed to.</param>
/// <param name="Loaded">False when it couldn't be loaded and isn't run.</param>
/// <param name="Error">Why it couldn't be loaded.</param>
/// <param name="ModuleSha256">SHA-256 of its module, when loaded.</param>
/// <param name="Enabled">False when it was turned off; it isn't run then and its blocks aren't offered.</param>
/// <param name="Blocks">Blocks it adds to the studio's library; empty when it isn't loaded or is turned off.</param>
public sealed record PluginInfo(
    string Id,
    string Name,
    string? Version,
    string? Description,
    IReadOnlyList<string> Events,
    bool Loaded,
    bool Enabled,
    string? Error,
    string? ModuleSha256,
    IReadOnlyList<PluginBlockDefinition> Blocks);

/// <summary>Lists the installed plugins and what they did recently.</summary>
public interface IPluginCoreService
{
    IReadOnlyList<PluginInfo> GetPlugins();

    /// <summary>The plugin's recent runs, newest first; null when no plugin has this id.</summary>
    IReadOnlyList<PluginRun>? GetRuns(string pluginId);

    /// <summary>Turns a plugin on or off at once; it is remembered across restarts.</summary>
    PluginChange SetEnabled(string pluginId, bool enabled);

    /// <summary>Deletes the plugin's files; it stops running at once.</summary>
    PluginChange Delete(string pluginId);

    /// <summary>The plugin as people see it, or null when no plugin has this id.</summary>
    PluginInfo? GetPlugin(string pluginId);
}

public sealed class PluginCoreService(IPluginCatalog catalog, IPluginRunLog runLog) : IPluginCoreService
{
    public IReadOnlyList<PluginInfo> GetPlugins() => catalog.Plugins.Select(ToInfo).ToList();

    public PluginInfo? GetPlugin(string pluginId) => catalog.Plugins.FirstOrDefault(p => p.Id == pluginId) is { } p ? ToInfo(p) : null;

    public IReadOnlyList<PluginRun>? GetRuns(string pluginId) =>
        catalog.Plugins.Any(p => p.Id == pluginId) ? runLog.GetRuns(pluginId) : null;

    public PluginChange SetEnabled(string pluginId, bool enabled) => catalog.SetEnabled(pluginId, enabled);

    public PluginChange Delete(string pluginId) => catalog.Delete(pluginId);

    private static PluginInfo ToInfo(InstalledPlugin p) => new(
        p.Id,
        p.Manifest?.Name ?? p.Folder,
        p.Manifest?.Version,
        p.Manifest?.Description,
        p.Manifest?.Events ?? [],
        p.IsLoaded,
        p.Enabled,
        p.Error,
        p.ModuleSha256,
        p.IsActive ? p.Manifest!.Blocks ?? [] : []);
}
