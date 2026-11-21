using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Documents.Validation;
using Forma.Application.Publishing;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>
/// Drafts and publishing: every draft of a website, checking a publish, and putting drafts live together.
/// Specification: <c>docs/design/drafts-and-publish.md</c>.
/// </summary>
public class PublishEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/drafts", GetDraftsAsync).WithName("GetDrafts").WithTags("Publishing");

        var group = app.MapGroup("/publish").WithTags("Publishing");

        group.MapPost("/check", CheckAsync)
            .WithName("CheckPublish")
            .WithValidation<PublishRequest>();

        group.MapPost("/", PublishAsync)
            .WithName("Publish")
            .WithValidation<PublishRequest>();
    }

    /// <summary>Lists every draft of a website, with what changed and when it is scheduled.</summary>
    /// <param name="websiteId">The website.</param>
    /// <param name="stateLoader">Service that reads the website's drafts.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The drafts: collections, then pages, then items.</returns>
    /// <response code="200">The website's drafts; empty when everything is live.</response>
    internal static async Task<Ok<IReadOnlyList<DraftDto>>> GetDraftsAsync(
        Guid websiteId,
        IWebsiteStateLoader stateLoader,
        CancellationToken cancellationToken)
    {
        var state = await stateLoader.LoadAsync(websiteId, cancellationToken);
        var drafts = DraftDescriber.Drafts(state).Select(d => d.ToDto()).ToList();

        return TypedResults.Ok<IReadOnlyList<DraftDto>>(drafts);
    }

    /// <summary>Checks a publish against the site as it would be, without publishing anything.</summary>
    /// <remarks>
    /// The answer lists what changes, the issues (errors block publishing), and <c>needs</c>: drafts not in the publish
    /// that would fix a blocking issue.
    /// </remarks>
    /// <param name="request">What to put live.</param>
    /// <param name="checkService">Service that checks publishes.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The check, or 404 when an id isn't one of the website's.</returns>
    /// <response code="200">The check.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="404">A collection, page or item isn't one of the website's.</response>
    internal static async Task<Results<Ok<PublishCheckDto>, NotFound>> CheckAsync(
        PublishRequest request,
        IPublishCheckService checkService,
        CancellationToken cancellationToken)
    {
        var check = await checkService.CheckAsync(request.ToSelection(), cancellationToken);

        return check is null ? TypedResults.NotFound() : TypedResults.Ok(check.ToDto());
    }

    /// <summary>Puts drafts live now, all at once: everything goes live, or nothing does.</summary>
    /// <param name="request">What to put live.</param>
    /// <param name="publishService">Service that publishes.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>What went live, a validation problem with the issues, or 404.</returns>
    /// <response code="200">Live; the body lists what went live and any warnings.</response>
    /// <response code="400">Something blocks the publish; <c>issues</c> and <c>needs</c> say what. Nothing was published.</response>
    /// <response code="404">A collection, page or item isn't one of the website's.</response>
    internal static async Task<Results<Ok<PublishResultDto>, ValidationProblem, NotFound>> PublishAsync(
        PublishRequest request,
        IPublishService publishService,
        CancellationToken cancellationToken)
    {
        var outcome = await publishService.PublishAsync(request.ToSelection(), cancellationToken: cancellationToken);

        return outcome switch
        {
            PublishOutcome.Published published => TypedResults.Ok(new PublishResultDto(
                published.PublishedAt,
                published.Check.Entities.Select(e => e.Entity.ToDto()).ToList(),
                published.Check.Issues.Where(i => i.Severity == IssueSeverity.Warning).ToDtos())),
            PublishOutcome.Blocked blocked => ToValidationProblem(blocked.Check),
            _ => TypedResults.NotFound(),
        };
    }

    /// <summary>
    /// A <c>400</c> problem with the blocking issues keyed by what they are about, and the full check under
    /// <c>issues</c> and <c>needs</c>.
    /// </summary>
    internal static ValidationProblem ToValidationProblem(PublishCheck check)
    {
        var errors = check.Issues.Where(i => i.Severity == IssueSeverity.Error).ToList();

        return TypedResults.ValidationProblem(
            errors.GroupBy(e => e.Entity?.Name ?? "publish").ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()),
            title: "Some changes need attention before they can go live.",
            extensions: new Dictionary<string, object?>
            {
                ["issues"] = check.Issues.ToDtos(),
                ["needs"] = check.ToDto().Needs,
            });
    }
}
