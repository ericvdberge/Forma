using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Services;
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
    }

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
