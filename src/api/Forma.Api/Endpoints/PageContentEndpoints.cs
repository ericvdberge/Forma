using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Entities;
using Forma.Application.Publishing;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.PageContents;
using Forma.Infrastructure.Repositories.Pages;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Forma.Api.Endpoints;

/// <summary>
/// Endpoints for a page's Design Studio content: read it, save its draft, discard the draft. A draft goes live with
/// <c>POST /publish</c>. Specification: <c>docs/design/page-content-api.md</c> and <c>docs/design/drafts-and-publish.md</c>.
/// </summary>
public class PageContentEndpoints : IEndpoints
{
    /// <summary>Largest accepted request body.</summary>
    private const long MaxRequestBytes = 1024 * 1024;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pages/{pageId:guid}/content").WithTags("Page content");

        group.MapGet("/", GetContentAsync).WithName("GetPageContent");

        group.MapPut("/draft", SaveDraftAsync)
            .WithName("SavePageDraft")
            .WithValidation<SavePageDraftRequest>()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        group.MapDelete("/draft", DiscardDraftAsync).WithName("DiscardPageDraft");
    }

    /// <summary>Gets a page's content: what visitors see, or the draft.</summary>
    /// <param name="pageId">The page's id.</param>
    /// <param name="version"><c>live</c> (the default) for what visitors see; <c>draft</c> for the draft, or the live version when there is none.</param>
    /// <param name="pageCoreService">Service that reads pages.</param>
    /// <param name="pageContentCoreService">Service that reads page content.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The page's content, 204 when it has none in that version, or 404 when the page does not exist.</returns>
    /// <response code="200">The page's content.</response>
    /// <response code="204">The page has no content in that version yet (never published, or no draft and nothing live).</response>
    /// <response code="404">No page exists with this id.</response>
    internal static async Task<Results<Ok<PageContentDto>, NoContent, NotFound>> GetContentAsync(
        Guid pageId,
        IPageCoreService pageCoreService,
        IPageContentCoreService pageContentCoreService,
        CancellationToken cancellationToken,
        ContentVersion version = ContentVersion.Live)
    {
        var page = await pageCoreService.GetPageAsync(new PageFilter { Id = pageId }, cancellationToken);
        if (page is null)
            return TypedResults.NotFound();

        var content = await pageContentCoreService.GetPageContentAsync(new PageContentFilter { PageId = page.Id }, cancellationToken);

        if (content?.ToDto(version) is not { } dto)
            return TypedResults.NoContent();

        return TypedResults.Ok(dto);
    }

    /// <summary>Saves a page's draft. Nothing changes on the site until the draft is published.</summary>
    /// <remarks>
    /// Only the document's structure is checked here (block types, ranges, safe links, allowed rich text). Content rules
    /// such as missing images or fields that don't exist are checked when it is published (<c>POST /publish/check</c>).
    /// </remarks>
    /// <param name="pageId">The page's id.</param>
    /// <param name="request">The document.</param>
    /// <param name="pageCoreService">Service that reads pages.</param>
    /// <param name="pageContentCoreService">Service that stores page content.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The draft, a validation problem, or 404.</returns>
    /// <response code="200">Saved; the body is the draft.</response>
    /// <response code="400">The document is malformed.</response>
    /// <response code="404">No page exists with this id.</response>
    /// <response code="413">The request body is larger than 1 MB.</response>
    internal static async Task<Results<Ok<PageContentDto>, ValidationProblem, NotFound>> SaveDraftAsync(
        Guid pageId,
        SavePageDraftRequest request,
        IPageCoreService pageCoreService,
        IPageContentCoreService pageContentCoreService,
        CancellationToken cancellationToken)
    {
        var page = await pageCoreService.GetPageAsync(new PageFilter { Id = pageId }, cancellationToken);
        if (page is null)
            return TypedResults.NotFound();

        var existing = await pageContentCoreService.GetPageContentAsync(new PageContentFilter { PageId = page.Id }, cancellationToken);

        var content = existing is null
            ? await pageContentCoreService.CreateAsync(new PageContentInsert { PageId = page.Id, DraftDocument = request.Document }, cancellationToken)
            : await pageContentCoreService.UpdateAsync(page.Id, new PageContentUpdate { DraftDocument = request.Document }, cancellationToken);

        return TypedResults.Ok(content!.ToDto(ContentVersion.Draft)!);
    }

    /// <summary>Discards a page's draft: the page goes back to its live version, and leaves any scheduled publish.</summary>
    /// <param name="pageId">The page's id.</param>
    /// <param name="pageCoreService">Service that reads pages.</param>
    /// <param name="pageContentCoreService">Service that stores page content.</param>
    /// <param name="scheduledPublishService">Service that takes the page out of its scheduled publish.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>204, or 404.</returns>
    /// <response code="204">The draft is gone (or there was none).</response>
    /// <response code="404">No page exists with this id.</response>
    internal static async Task<Results<NoContent, NotFound>> DiscardDraftAsync(
        Guid pageId,
        IPageCoreService pageCoreService,
        IPageContentCoreService pageContentCoreService,
        IScheduledPublishService scheduledPublishService,
        CancellationToken cancellationToken)
    {
        var page = await pageCoreService.GetPageAsync(new PageFilter { Id = pageId }, cancellationToken);
        if (page is null)
            return TypedResults.NotFound();

        await pageContentCoreService.UpdateAsync(page.Id, new PageContentUpdate { ClearDraft = true }, cancellationToken);

        await scheduledPublishService.RemoveAsync(page.WebsiteId, DraftKind.Page, page.Id, cancellationToken);

        return TypedResults.NoContent();
    }
}
