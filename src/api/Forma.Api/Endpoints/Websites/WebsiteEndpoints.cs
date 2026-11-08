using Forma.Api.Abstractions;
using Forma.Api.Dtos.Websites;
using Forma.Api.Mappers.Websites;
using Forma.Application.Services.Websites;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints.Websites;

public class WebsiteEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/websites").WithTags("Websites");

        group.MapGet("/", GetWebsitesAsync).WithName("GetWebsites");
    }

    private static async Task<Ok<IReadOnlyList<WebsiteDto>>> GetWebsitesAsync(
        IWebsiteCoreService websiteCoreService,
        CancellationToken cancellationToken)
    {
        var websites = await websiteCoreService.GetWebsitesAsync(cancellationToken);
        return TypedResults.Ok(websites.ToDtos());
    }
}
