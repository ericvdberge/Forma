using Forma.Infrastructure.Plugins;

namespace Forma.Api.Dtos;

/// <summary>An installed plugin.</summary>
/// <param name="Id">The plugin's id, e.g. <c>secure-links</c>.</param>
/// <param name="Name">The plugin's name.</param>
/// <param name="Version">The plugin's version; null when its manifest couldn't be read.</param>
/// <param name="Description">What it does.</param>
/// <param name="Events">The events it runs for, e.g. <c>page.published</c> or the <c>page.check</c> hook.</param>
/// <param name="Loaded">False when it couldn't be loaded; it isn't run then.</param>
/// <param name="Error">Why it couldn't be loaded.</param>
/// <param name="ModuleSha256">SHA-256 of its WebAssembly module, to tell builds apart.</param>
public record PluginDto(
    string Id,
    string Name,
    string? Version,
    string? Description,
    IReadOnlyList<string> Events,
    bool Loaded,
    string? Error,
    string? ModuleSha256);

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
