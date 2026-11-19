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
public sealed record PluginInfo(
    string Id,
    string Name,
    string? Version,
    string? Description,
    IReadOnlyList<string> Events,
    bool Loaded,
    string? Error,
    string? ModuleSha256);

/// <summary>Lists the installed plugins and what they did recently.</summary>
public interface IPluginCoreService
{
    IReadOnlyList<PluginInfo> GetPlugins();

    /// <summary>The plugin's recent runs, newest first; null when no plugin has this id.</summary>
    IReadOnlyList<PluginRun>? GetRuns(string pluginId);
}

public sealed class PluginCoreService(IPluginCatalog catalog, IPluginRunLog runLog) : IPluginCoreService
{
    public IReadOnlyList<PluginInfo> GetPlugins() => catalog.Plugins.Select(p => new PluginInfo(
        p.Manifest?.Id ?? p.Folder,
        p.Manifest?.Name ?? p.Folder,
        p.Manifest?.Version,
        p.Manifest?.Description,
        p.Manifest?.Events ?? [],
        p.IsLoaded,
        p.Error,
        p.ModuleSha256)).ToList();

    public IReadOnlyList<PluginRun>? GetRuns(string pluginId) =>
        catalog.Plugins.Any(p => (p.Manifest?.Id ?? p.Folder) == pluginId) ? runLog.GetRuns(pluginId) : null;
}
