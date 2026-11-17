using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>Endpoints for a website's pages.</summary>
public class PageEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pages").WithTags("Pages");

        group.MapGet("/", GetPagesAsync).WithName("GetPages");
    }

    /// <summary>Lists the pages of a website, most recently updated first.</summary>
    /// <param name="websiteId">The website whose pages are listed.</param>
    /// <param name="pageCoreService">Service that reads the pages.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The website's pages, or an empty list when it has none.</returns>
    /// <response code="200">The website's pages, most recently updated first.</response>
    internal static async Task<Ok<IReadOnlyList<PageDto>>> GetPagesAsync(
        Guid websiteId,
        IPageCoreService pageCoreService,
        CancellationToken cancellationToken)
    {
        var pages = await pageCoreService.GetPagesAsync(websiteId, cancellationToken);
        return TypedResults.Ok(pages.ToDtos());
    }
}
