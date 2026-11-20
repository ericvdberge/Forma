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
        plugin.Enabled,
        plugin.Error,
        plugin.ModuleSha256,
        plugin.Blocks.Select(b => new PluginBlockDto(
            b.Id,
            b.Label,
            b.Description,
            b.Icon,
            b.Template,
            b.Fields.Select(f => new PluginBlockFieldDto(
                f.Key, f.Label, f.Kind, f.Default, f.Options, f.Placeholder, f.Min, f.Max, f.Step, f.Unit, f.Advanced)).ToList())).ToList());

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
