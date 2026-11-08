using Forma.Api.Abstractions;
using Forma.Api.Dtos.Pages;
using Forma.Api.Mappers.Pages;
using Forma.Application.Filters.Pages;
using Forma.Application.Services.Pages;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints.Pages;

public class PageEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pages").WithTags("Pages");

        group.MapGet("/", GetPagesAsync).WithName("GetPages");
    }

    /// <summary>Lists pages. Every query parameter is optional and narrows the result when set.</summary>
    private static async Task<Ok<IReadOnlyList<PageDto>>> GetPagesAsync(
        [AsParameters] PageFilter filter,
        IPageCoreService pageCoreService,
        CancellationToken cancellationToken)
    {
        var pages = await pageCoreService.GetPagesAsync(filter, cancellationToken);
        return TypedResults.Ok(pages.ToDtos());
    }
}
