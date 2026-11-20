using System.Collections.Concurrent;
using Forma.Infrastructure.Plugins;

namespace Forma.Application.Plugins;

/// <summary>One time a plugin ran.</summary>
/// <param name="Id">Unique id of this run.</param>
/// <param name="PluginId">The plugin.</param>
/// <param name="Event">The event it ran for.</param>
/// <param name="EventId">The event's id, the same for every plugin that ran for it.</param>
/// <param name="StartedAt">When it started (UTC).</param>
/// <param name="Invocation">What happened.</param>
public sealed record PluginRun(Guid Id, string PluginId, string Event, Guid EventId, DateTime StartedAt, PluginInvocation Invocation);

/// <summary>The last runs of every plugin, in memory, so people can see what their plugins did and why one failed.</summary>
public interface IPluginRunLog
{
    void Add(PluginRun run);

    /// <summary>The plugin's recent runs, newest first.</summary>
    IReadOnlyList<PluginRun> GetRuns(string pluginId);
}

public sealed class PluginRunLog : IPluginRunLog
{
    /// <summary>Runs kept per plugin; older ones are dropped.</summary>
    public const int RunsPerPlugin = 25;

    private readonly ConcurrentDictionary<string, LinkedList<PluginRun>> _runs = new();

    public void Add(PluginRun run)
    {
        var runs = _runs.GetOrAdd(run.PluginId, _ => new());
        lock (runs)
        {
            runs.AddFirst(run);
            while (runs.Count > RunsPerPlugin)
            {
                runs.RemoveLast();
            }
        }
    }

    public IReadOnlyList<PluginRun> GetRuns(string pluginId)
    {
        if (!_runs.TryGetValue(pluginId, out var runs))
        {
            return [];
        }

        lock (runs)
        {
            return runs.ToList();
        }
    }
}
