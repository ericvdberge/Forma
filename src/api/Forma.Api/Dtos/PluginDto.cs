using System.Text.Json;
using Forma.Infrastructure.Plugins;

namespace Forma.Api.Dtos;

/// <summary>An installed plugin.</summary>
/// <param name="Id">The plugin's id, e.g. <c>secure-links</c>.</param>
/// <param name="Name">The plugin's name.</param>
/// <param name="Version">The plugin's version; null when its manifest couldn't be read.</param>
/// <param name="Description">What it does.</param>
/// <param name="Events">The events it runs for, e.g. <c>page.published</c> or the <c>page.check</c> hook.</param>
/// <param name="Loaded">False when it couldn't be loaded; it isn't run then.</param>
/// <param name="Enabled">False when it was turned off in the app; it isn't run then and its blocks aren't offered.</param>
/// <param name="Error">Why it couldn't be loaded.</param>
/// <param name="ModuleSha256">SHA-256 of its WebAssembly module, to tell builds apart.</param>
/// <param name="Blocks">Blocks it adds to the studio's library; empty when it isn't loaded.</param>
public record PluginDto(
    string Id,
    string Name,
    string? Version,
    string? Description,
    IReadOnlyList<string> Events,
    bool Loaded,
    bool Enabled,
    string? Error,
    string? ModuleSha256,
    IReadOnlyList<PluginBlockDto> Blocks);

/// <summary>A block a plugin adds to the studio. Stored in a page as <c>{ type: "plugin", props: { plugin, block, values } }</c>.</summary>
/// <param name="Id">The block's id within the plugin.</param>
/// <param name="Label">Its name in the library.</param>
/// <param name="Description">One line about it.</param>
/// <param name="Icon">A Lucide icon name, e.g. <c>shopping-bag</c>.</param>
/// <param name="Template">Restricted HTML with <c>{{key}}</c> slots, checked when the plugin loaded.</param>
/// <param name="Fields">The inspector fields, in order.</param>
public record PluginBlockDto(
    string Id,
    string Label,
    string? Description,
    string? Icon,
    string Template,
    IReadOnlyList<PluginBlockFieldDto> Fields);

/// <summary>An inspector field of a plugin block; its value is stored under <c>props.values[key]</c>.</summary>
/// <param name="Key">The value's name, also its <c>{{key}}</c> slot in the template.</param>
/// <param name="Label">The field's label in the inspector.</param>
/// <param name="Kind">text, textarea, url, image, color, number, slider, toggle, select or segmented.</param>
/// <param name="Default">The value of a new block: a string, number or boolean.</param>
/// <param name="Options">The choices of a select or segmented field.</param>
/// <param name="Placeholder">Hint shown in an empty text field.</param>
/// <param name="Min">Lowest value of a number or slider.</param>
/// <param name="Max">Highest value of a number or slider.</param>
/// <param name="Step">Step of a number or slider.</param>
/// <param name="Unit">Unit shown next to a slider's value, e.g. <c>px</c>.</param>
/// <param name="Advanced">Hidden in the studio's Simple mode.</param>
public record PluginBlockFieldDto(
    string Key,
    string Label,
    string Kind,
    JsonElement? Default,
    IReadOnlyList<PluginBlockFieldOption>? Options,
    string? Placeholder,
    double? Min,
    double? Max,
    double? Step,
    string? Unit,
    bool Advanced);

/// <summary>Turns a plugin on or off.</summary>
/// <param name="Enabled">True to run it and offer its blocks; false to keep it installed but stop it.</param>
public record SetPluginEnabledRequest(bool Enabled);

/// <summary>One time a plugin ran.</summary>
/// <param name="Id">The run's id.</param>
/// <param name="Event">The event it ran for.</param>
/// <param name="EventId">The event's id; plugins that ran for the same event share it.</param>
/// <param name="StartedAt">When it started (UTC).</param>
/// <param name="Succeeded">False when it crashed, hit a sandbox limit or returned bad output.</param>
/// <param name="Error">Why it failed.</param>
/// <param name="DurationMs">How long it took, in milliseconds.</param>
/// <param name="FuelConsumed">Roughly how many WebAssembly instructions it ran.</param>
/// <param name="Logs">What it logged.</param>
/// <param name="DroppedLogLines">Lines it logged past the limit.</param>
public record PluginRunDto(
    Guid Id,
    string Event,
    Guid EventId,
    DateTime StartedAt,
    bool Succeeded,
    string? Error,
    double DurationMs,
    long FuelConsumed,
    IReadOnlyList<PluginLogLineDto> Logs,
    int DroppedLogLines);

/// <summary>A line a plugin logged.</summary>
public record PluginLogLineDto(PluginLogLevel Level, string Message);
