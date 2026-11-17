using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Forma.Api.Endpoints;

/// <summary>
/// Endpoints for a page's Design Studio content: its draft and published document.
/// Specification: <c>docs/design/page-content-api.md</c>.
/// </summary>
public class PageContentEndpoints : IEndpoints
{
    /// <summary>Largest accepted draft request body.</summary>
    private const long MaxDraftRequestBytes = 1024 * 1024;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pages/{pageId:guid}").WithTags("Page content");

        group.MapGet("/content", GetContentAsync).WithName("GetPageContent");
        group.MapPut("/draft", SaveDraftAsync)
            .WithName("SavePageDraft")
            .WithValidation<SaveDraftRequest>()
            .WithMetadata(new RequestSizeLimitAttribute(MaxDraftRequestBytes))
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        // Phase 2: POST /check and POST /publish.
    }

    /// <summary>Gets a page's draft and published document.</summary>
    /// <remarks>A page without saved content gets an empty draft at revision 0 and no published version.</remarks>
    /// <param name="pageId">The page's id.</param>
    /// <param name="pageContentCoreService">Service that reads the page's documents.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The page's content, or 404 when the page does not exist.</returns>
    /// <response code="200">The page's draft and published document.</response>
    /// <response code="404">No page exists with this id.</response>
    internal static async Task<Results<Ok<PageContentDto>, NotFound>> GetContentAsync(
        Guid pageId,
        IPageContentCoreService pageContentCoreService,
        CancellationToken cancellationToken)
    {
        var content = await pageContentCoreService.GetContentAsync(pageId, cancellationToken);
        return content is null ? TypedResults.NotFound() : TypedResults.Ok(content.ToDto());
    }

    /// <summary>Replaces a page's draft. The Design Studio's autosave calls this.</summary>
    /// <remarks>
    /// The document must be complete and pass the structural rules (block types, ranges, safe links, allowed rich text).
    /// Saving a document identical to the current draft returns the same revision, so retries are harmless.
    /// </remarks>
    /// <param name="pageId">The page's id.</param>
    /// <param name="request">The new draft and the revision it was edited from.</param>
    /// <param name="pageContentCoreService">Service that stores the draft.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The new revision, a validation problem, 404, or 409 with the current content.</returns>
    /// <response code="200">The draft was saved (or was already identical).</response>
    /// <response code="400">The document is malformed or breaks a structural rule.</response>
    /// <response code="404">No page exists with this id.</response>
    /// <response code="409">The draft was saved elsewhere since <c>baseRevision</c>; the body is the current content.</response>
    /// <response code="413">The request body is larger than 1 MB.</response>
    /// <exception cref="InvalidOperationException">The core service returned a result this endpoint does not handle.</exception>
    internal static async Task<Results<Ok<SavedDraftDto>, ValidationProblem, NotFound, Conflict<PageContentDto>>> SaveDraftAsync(
        Guid pageId,
        SaveDraftRequest request,
        IPageContentCoreService pageContentCoreService,
        CancellationToken cancellationToken)
    {
        var result = await pageContentCoreService.SaveDraftAsync(pageId, request.Document, request.BaseRevision, cancellationToken);

        return result switch
        {
            SaveDraftResult.Saved saved => TypedResults.Ok(saved.ToDto()),
            SaveDraftResult.PageNotFound => TypedResults.NotFound(),
            SaveDraftResult.Conflict conflict => TypedResults.Conflict(conflict.Current.ToDto()),
            _ => throw new InvalidOperationException($"Unhandled save result {result.GetType().Name}."),
        };
    }
}
