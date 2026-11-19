using Forma.Api.Dtos;
using Forma.Application.Plugins;
using Forma.Application.Services;

namespace Forma.Api.Mappers;

public static class PluginDtoMapper
{
    public static PluginDto ToDto(this PluginInfo plugin) => new(
        plugin.Id,
        plugin.Name,
        plugin.Version,
        plugin.Description,
        plugin.Events,
        plugin.Loaded,
        plugin.Error,
        plugin.ModuleSha256);

    public static PluginRunDto ToDto(this PluginRun run) => new(
        run.Id,
        run.Event,
        run.EventId,
        run.StartedAt,
        run.Invocation.Succeeded,
        run.Invocation.Error,
        Math.Round(run.Invocation.Duration.TotalMilliseconds, 2),
        run.Invocation.FuelConsumed,
        run.Invocation.Logs.Select(l => new PluginLogLineDto(l.Level, l.Message)).ToList(),
        run.Invocation.DroppedLogLines);
}
