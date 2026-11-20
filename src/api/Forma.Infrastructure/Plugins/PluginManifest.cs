namespace Forma.Infrastructure.Plugins;

/// <summary>
/// A plugin's <c>plugin.json</c>: who it is and which events it handles. Specification: <c>docs/design/plugins.md</c>.
/// </summary>
/// <param name="Id">Stable id, also the folder name: lowercase letters, digits and dashes, e.g. <c>secure-links</c>.</param>
/// <param name="Name">Shown to people, e.g. "Secure links".</param>
/// <param name="Version">The plugin's own version, e.g. <c>1.0.0</c>.</param>
/// <param name="Description">One sentence about what it does.</param>
/// <param name="Events">The events it is called for, e.g. <c>page.published</c> or the <c>page.check</c> hook.</param>
/// <param name="Module">The WebAssembly file, relative to the plugin's folder.</param>
/// <param name="Blocks">Blocks the plugin adds to the studio's library; see <see cref="PluginBlockDefinition"/>.</param>
public sealed record PluginManifest(
    string Id,
    string Name,
    string Version,
    IReadOnlyList<string> Events,
    string? Description = null,
    string Module = "plugin.wasm",
    IReadOnlyList<PluginBlockDefinition>? Blocks = null);
