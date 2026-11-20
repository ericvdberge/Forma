using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.PageContents;
using Forma.Infrastructure.Repositories.Pages;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Forma.Api.Endpoints;

/// <summary>
/// Endpoints for a page's Design Studio content. Specification: <c>docs/design/page-content-api.md</c>.
/// </summary>
public class PageContentEndpoints : IEndpoints
{
    /// <summary>Largest accepted request body.</summary>
    private const long MaxRequestBytes = 1024 * 1024;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pages/{pageId:guid}/content").WithTags("Page content");

        group.MapGet("/", GetContentAsync).WithName("GetPageContent");

        group.MapPut("/", PublishContentAsync)
            .WithName("PublishPageContent")
            .WithValidation<PublishPageContentRequest>()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);
    }

    /// <summary>Gets a page's content.</summary>
    /// <param name="pageId">The page's id.</param>
    /// <param name="pageCoreService">Service that reads pages.</param>
    /// <param name="pageContentCoreService">Service that reads page content.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The page's content, 204 when it has none yet, or 404 when the page does not exist.</returns>
    /// <response code="200">The page's content.</response>
    /// <response code="204">The page exists but has no content yet.</response>
    /// <response code="404">No page exists with this id.</response>
    internal static async Task<Results<Ok<PageContentDto>, NoContent, NotFound>> GetContentAsync(
        Guid pageId,
        IPageCoreService pageCoreService,
        IPageContentCoreService pageContentCoreService,
        CancellationToken cancellationToken)
    {
        var page = await pageCoreService.GetPageAsync(new PageFilter { Id = pageId }, cancellationToken);
        if (page is null)
            return TypedResults.NotFound();

        var content = await pageContentCoreService.GetPageContentAsync(new PageContentFilter { PageId = page.Id }, cancellationToken);
        if (content is null)
            return TypedResults.NoContent();

        return TypedResults.Ok(content.ToDto());
    }

    /// <summary>Publishes a page: stores the document and marks the content published. The studio calls this on Publish.</summary>
    /// <remarks>
    /// The document must pass the structural rules (block types, ranges, safe links, allowed rich text) and the content
    /// rules (no missing images, links that go somewhere). Errors are answered with a validation problem whose
    /// <c>issues</c> point at the sections and blocks to fix; warnings don't block and come back with the result.
    /// </remarks>
    /// <param name="pageId">The page's id.</param>
    /// <param name="request">The document to publish.</param>
    /// <param name="pageCoreService">Service that reads pages.</param>
    /// <param name="pageContentCoreService">Service that stores page content.</param>
    /// <param name="validationService">Service that checks the content rules.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The published content and its warnings, a validation problem, or 404.</returns>
    /// <response code="200">Published; the body is the content and any warnings.</response>
    /// <response code="400">The document is malformed or has errors to fix first.</response>
    /// <response code="404">No page exists with this id.</response>
    /// <response code="413">The request body is larger than 1 MB.</response>
    internal static async Task<Results<Ok<PublishedPageContentDto>, ValidationProblem, NotFound>> PublishContentAsync(
        Guid pageId,
        PublishPageContentRequest request,
        IPageCoreService pageCoreService,
        IPageContentCoreService pageContentCoreService,
        IPageContentValidationService validationService,
        CancellationToken cancellationToken)
    {
        var page = await pageCoreService.GetPageAsync(new PageFilter { Id = pageId }, cancellationToken);
        if (page is null)
            return TypedResults.NotFound();

        var issues = await validationService.ValidateAsync(request.Document, page.WebsiteId, cancellationToken);
        var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
        if (errors.Count > 0)
            return ToValidationProblem(errors);

        var existing = await pageContentCoreService.GetPageContentAsync(new PageContentFilter { PageId = page.Id }, cancellationToken);

        var content = existing is null
            ? await pageContentCoreService.CreateAsync(
                new PageContentInsert { PageId = page.Id, Document = request.Document, Status = PageContentStatus.Published },
                cancellationToken)
            : await pageContentCoreService.UpdateAsync(
                page.Id,
                new PageContentUpdate { Document = request.Document, Status = PageContentStatus.Published },
                cancellationToken);

        var warnings = issues.Where(i => i.Severity == IssueSeverity.Warning).ToList();

        return TypedResults.Ok(new PublishedPageContentDto(content!.ToDto(), warnings.ToDtos()));
    }

    /// <summary>
    /// A <c>400</c> problem with the errors keyed by where they are (<c>sectionId.blockId.field</c>), and the full
    /// issues under <c>issues</c> so the studio can select what to fix.
    /// </summary>
    private static ValidationProblem ToValidationProblem(IReadOnlyList<ContentIssue> errors) =>
        TypedResults.ValidationProblem(
            errors.GroupBy(e => e.Location).ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()),
            title: "The page has errors to fix before it can be published.",
            extensions: new Dictionary<string, object?> { ["issues"] = errors.ToDtos() });
}
