using System.Text.Json;
using Forma.Application.Documents;
using Forma.Infrastructure.Plugins;
using Microsoft.Extensions.Logging;

namespace Forma.Application.Plugins;

/// <summary>Runs the plugins subscribed to an event, one after the other, each in its own sandbox.</summary>
public interface IPluginRunner
{
    /// <summary>
    /// Runs every loaded plugin that subscribed to <paramref name="envelope"/>'s event, recording each run.
    /// A plugin that fails doesn't stop the others.
    /// </summary>
    IReadOnlyList<(InstalledPlugin Plugin, PluginRun Run)> Run(PluginEventEnvelope envelope);
}

public sealed class PluginRunner(
    IPluginCatalog catalog,
    IWasmSandbox sandbox,
    IPluginRunLog runLog,
    TimeProvider timeProvider,
    ILogger<PluginRunner> logger) : IPluginRunner
{
    public IReadOnlyList<(InstalledPlugin Plugin, PluginRun Run)> Run(PluginEventEnvelope envelope)
    {
        var subscribed = catalog.Plugins.Where(p => p.IsLoaded && p.Manifest!.Events.Contains(envelope.Event)).ToList();
        if (subscribed.Count == 0)
        {
            return [];
        }

        // Plugins get the same JSON the API sends: camelCase, string enums, documents exactly as stored.
        var input = JsonSerializer.Serialize(envelope, PageDocumentJson.Options);
        var runs = new List<(InstalledPlugin, PluginRun)>(subscribed.Count);
        foreach (var plugin in subscribed)
        {
            var startedAt = timeProvider.GetUtcNow().UtcDateTime;
            var invocation = sandbox.Invoke(plugin.Module!, input);
            var run = new PluginRun(Guid.NewGuid(), plugin.Manifest!.Id, envelope.Event, envelope.Id, startedAt, invocation);
            runLog.Add(run);
            Report(run);
            runs.Add((plugin, run));
        }

        return runs;
    }

    private void Report(PluginRun run)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["PluginId"] = run.PluginId, ["PluginEvent"] = run.Event });
        foreach (var line in run.Invocation.Logs)
        {
            logger.Log(line.Level switch
            {
                PluginLogLevel.Debug => LogLevel.Debug,
                PluginLogLevel.Warning => LogLevel.Warning,
                PluginLogLevel.Error => LogLevel.Error,
                _ => LogLevel.Information,
            }, "[plugin {PluginId}] {Message}", run.PluginId, line.Message);
        }

        if (run.Invocation.Succeeded)
        {
            logger.LogDebug("Plugin {PluginId} handled {Event} in {Duration:0.0} ms", run.PluginId, run.Event, run.Invocation.Duration.TotalMilliseconds);
        }
        else
        {
            logger.LogWarning("Plugin {PluginId} failed on {Event}: {Error}", run.PluginId, run.Event, run.Invocation.Error);
        }
    }
}
