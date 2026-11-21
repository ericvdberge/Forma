using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Publishing;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>
/// Scheduled publishes: drafts the server puts live at a set time. Specification: <c>docs/design/drafts-and-publish.md</c> § 4.3.
/// </summary>
public class ScheduledPublishEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/scheduled-publishes").WithTags("Publishing");

        group.MapGet("/", GetScheduledPublishesAsync).WithName("GetScheduledPublishes");

        group.MapPost("/", ScheduleAsync)
            .WithName("SchedulePublish")
            .WithValidation<SchedulePublishRequest>();

        group.MapPut("/{id:guid}", ChangeAsync)
            .WithName("UpdateScheduledPublish")
            .WithValidation<UpdateScheduledPublishRequest>();

        group.MapDelete("/{id:guid}", CancelAsync).WithName("CancelScheduledPublish");
    }

    /// <summary>Lists a website's upcoming and failed scheduled publishes, soonest first.</summary>
    /// <param name="websiteId">The website.</param>
    /// <param name="service">Service that reads scheduled publishes.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The scheduled publishes.</returns>
    /// <response code="200">Pending and failed publishes.</response>
    internal static async Task<Ok<IReadOnlyList<ScheduledPublishDto>>> GetScheduledPublishesAsync(
        Guid websiteId,
        IScheduledPublishService service,
        CancellationToken cancellationToken)
    {
        var publishes = await service.GetScheduledPublishesAsync(websiteId, cancellationToken);

        return TypedResults.Ok<IReadOnlyList<ScheduledPublishDto>>(publishes.Select(p => p.ToDto()).ToList());
    }

    /// <summary>Schedules drafts to go live at a time. The publish is checked now, and again when it runs.</summary>
    /// <remarks>A draft can be in one scheduled publish at a time. The drafts go live as they are when it runs.</remarks>
    /// <param name="request">When, and what.</param>
    /// <param name="service">Service that schedules publishes.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The scheduled publish, a validation problem, or 404.</returns>
    /// <response code="201">Scheduled.</response>
    /// <response code="400">The time is in the past, a draft is already scheduled, or something blocks the publish.</response>
    /// <response code="404">A collection, page or item isn't one of the website's.</response>
    internal static async Task<Results<Created<ScheduledPublishDto>, ValidationProblem, NotFound>> ScheduleAsync(
        SchedulePublishRequest request,
        IScheduledPublishService service,
        CancellationToken cancellationToken)
    {
        var selection = new PublishRequest(request.WebsiteId, request.Collections, request.Pages, request.Items).ToSelection();
        var outcome = await service.ScheduleAsync(selection, request.PublishAt.UtcDateTime, cancellationToken);

        return outcome switch
        {
            ScheduleOutcome.Scheduled scheduled => TypedResults.Created($"/scheduled-publishes/{scheduled.Details.Publish.Id}", scheduled.Details.ToDto()),
            ScheduleOutcome.Invalid invalid => TypedResults.ValidationProblem(invalid.Errors),
            ScheduleOutcome.Blocked blocked => PublishEndpoints.ToValidationProblem(blocked.Check),
            _ => TypedResults.NotFound(),
        };
    }

    /// <summary>Changes a scheduled publish's time, or what is in it. A failed publish that is changed is scheduled again.</summary>
    /// <param name="id">The scheduled publish's id.</param>
    /// <param name="request">The new time and/or contents.</param>
    /// <param name="service">Service that changes scheduled publishes.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The scheduled publish, a validation problem, or 404.</returns>
    /// <response code="200">Changed.</response>
    /// <response code="400">The time is in the past, a draft is already scheduled, or something blocks the publish.</response>
    /// <response code="404">No pending or failed publish with this id, or an id isn't one of its website's.</response>
    internal static async Task<Results<Ok<ScheduledPublishDto>, ValidationProblem, NotFound>> ChangeAsync(
        Guid id,
        UpdateScheduledPublishRequest request,
        IScheduledPublishService service,
        CancellationToken cancellationToken)
    {
        Func<Guid, PublishSelection>? selection = request.Collections is null
            ? null
            : websiteId => new PublishRequest(websiteId, request.Collections, request.Pages!, request.Items!).ToSelection();

        var outcome = await service.ChangeAsync(id, request.PublishAt?.UtcDateTime, selection, cancellationToken);

        return outcome switch
        {
            ScheduleOutcome.Scheduled scheduled => TypedResults.Ok(scheduled.Details.ToDto()),
            ScheduleOutcome.Invalid invalid => TypedResults.ValidationProblem(invalid.Errors),
            ScheduleOutcome.Blocked blocked => PublishEndpoints.ToValidationProblem(blocked.Check),
            _ => TypedResults.NotFound(),
        };
    }

    /// <summary>Cancels a scheduled publish. Its drafts stay drafts.</summary>
    /// <param name="id">The scheduled publish's id.</param>
    /// <param name="service">Service that cancels scheduled publishes.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>204, or 404.</returns>
    /// <response code="204">Cancelled.</response>
    /// <response code="404">No pending or failed publish with this id.</response>
    internal static async Task<Results<NoContent, NotFound>> CancelAsync(
        Guid id,
        IScheduledPublishService service,
        CancellationToken cancellationToken)
    {
        var cancelled = await service.CancelAsync(id, cancellationToken);

        return cancelled ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
