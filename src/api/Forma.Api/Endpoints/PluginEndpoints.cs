using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Services;
using Forma.Infrastructure.Plugins;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>
/// Endpoints for installed plugins: WebAssembly modules that run in a sandbox when pages are saved, checked or published.
/// Specification: <c>docs/design/plugins.md</c>.
/// </summary>
public class PluginEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/plugins").WithTags("Plugins");

        group.MapGet("/", GetPlugins).WithName("GetPlugins");
        group.MapGet("/{pluginId}/runs", GetRuns).WithName("GetPluginRuns");
        group.MapPut("/{pluginId}/enabled", SetEnabled).WithName("SetPluginEnabled");
        group.MapDelete("/{pluginId}", Delete).WithName("DeletePlugin");
    }

    /// <summary>Turns a plugin on or off, at once and across restarts.</summary>
    /// <remarks>
    /// A plugin that is off stays installed: it isn't run for any event and its blocks aren't offered in the studio.
    /// Pages that already use its blocks keep them; the publish check warns that they won't show. Remembered in
    /// <c>plugins.state.json</c> in the plugins folder.
    /// </remarks>
    /// <param name="pluginId">The plugin's id.</param>
    /// <param name="request">Whether it should run.</param>
    /// <param name="pluginCoreService">Service that changes the plugin.</param>
    /// <response code="200">The plugin, turned on or off.</response>
    /// <response code="404">No plugin is installed with this id.</response>
    /// <response code="409">The plugins folder isn't writable, so the change couldn't be saved.</response>
    internal static Results<Ok<PluginDto>, NotFound, ProblemHttpResult> SetEnabled(
        string pluginId, SetPluginEnabledRequest request, IPluginCoreService pluginCoreService) =>
        pluginCoreService.SetEnabled(pluginId, request.Enabled) switch
        {
            PluginChange.Done => TypedResults.Ok(pluginCoreService.GetPlugin(pluginId)!.ToDto()),
            PluginChange.NotFound => TypedResults.NotFound(),
            _ => NotWritable(),
        };

    /// <summary>Deletes a plugin: its folder is removed and it stops running at once.</summary>
    /// <remarks>Pages that use its blocks keep them; the publish check warns that they won't show.</remarks>
    /// <param name="pluginId">The plugin's id.</param>
    /// <param name="pluginCoreService">Service that deletes the plugin.</param>
    /// <response code="204">The plugin was deleted.</response>
    /// <response code="404">No plugin is installed with this id.</response>
    /// <response code="409">The plugins folder isn't writable, so the plugin couldn't be deleted.</response>
    internal static Results<NoContent, NotFound, ProblemHttpResult> Delete(string pluginId, IPluginCoreService pluginCoreService) =>
        pluginCoreService.Delete(pluginId) switch
        {
            PluginChange.Done => TypedResults.NoContent(),
            PluginChange.NotFound => TypedResults.NotFound(),
            _ => NotWritable(),
        };

    private static ProblemHttpResult NotWritable() => TypedResults.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "The plugins folder isn't writable",
        detail: "The change couldn't be saved. Make sure the API can write to its plugins folder (with Docker, don't mount it read-only).");

    /// <summary>Lists the installed plugins, including ones that couldn't be loaded and why.</summary>
    /// <param name="pluginCoreService">Service that reads the installed plugins.</param>
    /// <returns>Every plugin folder, in name order.</returns>
    /// <response code="200">The installed plugins; empty when there are none.</response>
    internal static Ok<IReadOnlyList<PluginDto>> GetPlugins(IPluginCoreService pluginCoreService) =>
        TypedResults.Ok<IReadOnlyList<PluginDto>>(pluginCoreService.GetPlugins().Select(p => p.ToDto()).ToList());

    /// <summary>Lists a plugin's recent runs, newest first, with what it logged and why a run failed.</summary>
    /// <remarks>Kept in memory: the last 25 runs per plugin since the API started.</remarks>
    /// <param name="pluginId">The plugin's id.</param>
    /// <param name="pluginCoreService">Service that reads the run history.</param>
    /// <returns>The runs, or 404 when no plugin has this id.</returns>
    /// <response code="200">The plugin's recent runs; empty when it hasn't run yet.</response>
    /// <response code="404">No plugin is installed with this id.</response>
    internal static Results<Ok<IReadOnlyList<PluginRunDto>>, NotFound> GetRuns(string pluginId, IPluginCoreService pluginCoreService) =>
        pluginCoreService.GetRuns(pluginId) is { } runs
            ? TypedResults.Ok<IReadOnlyList<PluginRunDto>>(runs.Select(r => r.ToDto()).ToList())
            : TypedResults.NotFound();
}
