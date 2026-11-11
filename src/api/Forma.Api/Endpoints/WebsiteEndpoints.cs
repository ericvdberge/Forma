using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>Endpoints for websites.</summary>
public class WebsiteEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/websites").WithTags("Websites");

        group.MapGet("/", GetWebsitesAsync).WithName("GetWebsites");
    }

    /// <summary>Lists all websites, most recently updated first.</summary>
    /// <param name="websiteCoreService">Service that reads the websites.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>All websites, or an empty list when there are none.</returns>
    /// <response code="200">All websites, most recently updated first.</response>
    internal static async Task<Ok<IReadOnlyList<WebsiteDto>>> GetWebsitesAsync(
        IWebsiteCoreService websiteCoreService,
        CancellationToken cancellationToken)
    {
        var websites = await websiteCoreService.GetWebsitesAsync(cancellationToken);
        return TypedResults.Ok(websites.ToDtos());
    }
}
