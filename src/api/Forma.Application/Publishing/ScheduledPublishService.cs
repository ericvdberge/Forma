using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Forma.Infrastructure.Repositories.ScheduledPublishEntries;
using Forma.Infrastructure.Repositories.ScheduledPublishes;

namespace Forma.Application.Publishing;

/// <summary>A scheduled publish with what it puts live, by name.</summary>
public sealed record ScheduledPublishDetails(ScheduledPublish Publish, IReadOnlyList<EntityRef> Entries);

/// <summary>Outcome of scheduling a publish or changing one.</summary>
public abstract record ScheduleOutcome
{
    private ScheduleOutcome() { }

    public sealed record Scheduled(ScheduledPublishDetails Details) : ScheduleOutcome;

    /// <summary>The request can't be scheduled; <paramref name="Errors"/> are keyed by property.</summary>
    public sealed record Invalid(IDictionary<string, string[]> Errors) : ScheduleOutcome;

    /// <summary>The check found errors.</summary>
    public sealed record Blocked(PublishCheck Check) : ScheduleOutcome;

    public sealed record NotFound : ScheduleOutcome;
}

/// <summary>
/// Scheduled publishes (<c>docs/design/drafts-and-publish.md</c> § 4.3): schedules drafts for a time, changes and cancels
/// them, and runs the ones that are due. The check runs when a publish is scheduled and again when it runs; a publish
/// that is blocked when it runs publishes nothing and is marked failed with its issues.
/// </summary>
public interface IScheduledPublishService
{
    /// <summary>The website's upcoming and failed publishes, soonest first.</summary>
    Task<IReadOnlyList<ScheduledPublishDetails>> GetScheduledPublishesAsync(Guid websiteId, CancellationToken cancellationToken = default);

    Task<ScheduleOutcome> ScheduleAsync(PublishSelection selection, DateTime publishAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the time and, when <paramref name="selection"/> is given, what is in it. A failed publish that is changed
    /// is pending again.
    /// </summary>
    Task<ScheduleOutcome> ChangeAsync(Guid id, DateTime? publishAt, Func<Guid, PublishSelection>? selection, CancellationToken cancellationToken = default);

    /// <summary>Cancels a pending or failed publish. Returns <c>false</c> when there is none with this id.</summary>
    Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Takes an entity out of the pending publish it is in, e.g. when its draft is discarded.</summary>
    Task RemoveAsync(Guid websiteId, DraftKind kind, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Runs every pending publish that is due, and returns how many there were.</summary>
    Task<int> RunDueAsync(CancellationToken cancellationToken = default);
}

public class ScheduledPublishService(
    IScheduledPublishCoreService publishCoreService,
    IScheduledPublishEntryCoreService entryCoreService,
    IWebsiteStateLoader stateLoader,
    IPublishCheckService checkService,
    IPublishService publishService,
    ITransactionRunner transactionRunner,
    TimeProvider timeProvider) : IScheduledPublishService
{
    private static readonly ScheduledPublishDaoStatus[] Pending = [ScheduledPublishDaoStatus.Pending];
    private static readonly ScheduledPublishDaoStatus[] PendingOrFailed = [ScheduledPublishDaoStatus.Pending, ScheduledPublishDaoStatus.Failed];

    public async Task<IReadOnlyList<ScheduledPublishDetails>> GetScheduledPublishesAsync(Guid websiteId, CancellationToken cancellationToken = default)
    {
        var publishes = await publishCoreService.GetScheduledPublishesAsync(
            new ScheduledPublishFilter { WebsiteId = websiteId, Statuses = PendingOrFailed },
            cancellationToken);

        var state = await stateLoader.LoadAsync(websiteId, cancellationToken);
        var details = new List<ScheduledPublishDetails>();

        foreach (var publish in publishes)
            details.Add(await DetailsAsync(publish, state, cancellationToken));

        return details;
    }

    public async Task<ScheduleOutcome> ScheduleAsync(PublishSelection selection, DateTime publishAt, CancellationToken cancellationToken = default)
    {
        var check = await checkService.CheckAsync(selection, cancellationToken);

        if (check is null)
            return new ScheduleOutcome.NotFound();

        if (Problems(check, publishAt, exceptPublishId: null) is { Count: > 0 } errors)
            return new ScheduleOutcome.Invalid(errors);

        if (!check.CanPublish)
            return new ScheduleOutcome.Blocked(check);

        var publish = await transactionRunner.RunAsync(async ct =>
        {
            var created = await publishCoreService.CreateAsync(new ScheduledPublishInsert { WebsiteId = selection.WebsiteId, PublishAt = publishAt }, ct);

            await entryCoreService.CreateAsync(Entries(created.Id, check), ct);

            return created;
        }, cancellationToken: cancellationToken);

        return new ScheduleOutcome.Scheduled(await DetailsAsync(publish, check.State, cancellationToken));
    }

    public async Task<ScheduleOutcome> ChangeAsync(Guid id, DateTime? publishAt, Func<Guid, PublishSelection>? selection, CancellationToken cancellationToken = default)
    {
        var publish = await publishCoreService.GetScheduledPublishAsync(new ScheduledPublishFilter { Id = id, Statuses = PendingOrFailed }, cancellationToken);

        if (publish is null)
            return new ScheduleOutcome.NotFound();

        var wanted = selection?.Invoke(publish.WebsiteId) ?? await SelectionOfAsync(publish, cancellationToken);
        var check = await checkService.CheckAsync(wanted, cancellationToken);

        if (check is null)
            return new ScheduleOutcome.NotFound();

        var time = publishAt ?? publish.PublishAt;

        if (Problems(check, time, exceptPublishId: publish.Id) is { Count: > 0 } errors)
            return new ScheduleOutcome.Invalid(errors);

        if (!check.CanPublish)
            return new ScheduleOutcome.Blocked(check);

        var changed = await transactionRunner.RunAsync(async ct =>
        {
            if (selection is not null)
                await ReplaceEntriesAsync(publish.Id, check, ct);

            return await publishCoreService.UpdateAsync(publish.Id, new ScheduledPublishUpdate
            {
                PublishAt = time,
                Status = ScheduledPublishStatus.Pending,
                Issues = [],
            }, ct);
        }, cancellationToken: cancellationToken);

        return new ScheduleOutcome.Scheduled(await DetailsAsync(changed!, check.State, cancellationToken));
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var publish = await publishCoreService.GetScheduledPublishAsync(new ScheduledPublishFilter { Id = id, Statuses = PendingOrFailed }, cancellationToken);

        if (publish is null)
            return false;

        await publishCoreService.UpdateAsync(publish.Id, new ScheduledPublishUpdate
        {
            Status = ScheduledPublishStatus.Cancelled,
            CompletedAt = timeProvider.GetUtcNow().UtcDateTime,
        }, cancellationToken);

        return true;
    }

    public async Task RemoveAsync(Guid websiteId, DraftKind kind, Guid id, CancellationToken cancellationToken = default)
    {
        var entries = await entryCoreService.GetScheduledPublishEntriesAsync(new ScheduledPublishEntryFilter
        {
            WebsiteId = websiteId,
            PublishStatuses = Pending,
            CollectionId = kind == DraftKind.Collection ? id : null,
            PageId = kind == DraftKind.Page ? id : null,
            CollectionItemId = kind == DraftKind.Item ? id : null,
        }, cancellationToken);

        foreach (var entry in entries)
            await RemoveEntryAsync(entry, cancellationToken);
    }

    public async Task<int> RunDueAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var due = await publishCoreService.GetScheduledPublishesAsync(new ScheduledPublishFilter { Statuses = Pending, DueBy = now }, cancellationToken);

        foreach (var publish in due)
            await RunAsync(publish, cancellationToken);

        return due.Count;
    }

    /// <summary>Publishes it now: <c>Published</c> when it went live, <c>Failed</c> with the issues when anything blocked it.</summary>
    private async Task RunAsync(ScheduledPublish publish, CancellationToken cancellationToken)
    {
        var selection = await SelectionOfAsync(publish, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (selection.IsEmpty)
        {
            await CompleteAsync(publish, ScheduledPublishStatus.Cancelled, [], now, cancellationToken);
            return;
        }

        PublishOutcome outcome;

        try
        {
            outcome = await publishService.PublishAsync(selection, publish.Id, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            outcome = new PublishOutcome.NotFound();
        }

        var (status, issues) = outcome switch
        {
            PublishOutcome.Published => (ScheduledPublishStatus.Published, []),
            PublishOutcome.Blocked blocked => (ScheduledPublishStatus.Failed, blocked.Check.Issues.Where(i => i.Severity == IssueSeverity.Error).ToList()),
            _ => (ScheduledPublishStatus.Failed, new List<PublishIssue> { FailedIssue(publish) }),
        };

        await CompleteAsync(publish, status, issues, now, cancellationToken);
    }

    private Task CompleteAsync(ScheduledPublish publish, ScheduledPublishStatus status, IReadOnlyList<PublishIssue> issues, DateTime now, CancellationToken cancellationToken) =>
        publishCoreService.UpdateAsync(publish.Id, new ScheduledPublishUpdate { Status = status, Issues = issues, CompletedAt = now }, cancellationToken);

    private static PublishIssue FailedIssue(ScheduledPublish publish) =>
        new(IssueSeverity.Error, "publish.failed", "Something went wrong while publishing. Nothing was published.", null);

    /// <summary>What a request to schedule gets wrong that the publish check doesn't look at: the time, empty drafts, and double scheduling.</summary>
    private Dictionary<string, string[]> Problems(PublishCheck check, DateTime publishAt, Guid? exceptPublishId)
    {
        var errors = new Dictionary<string, string[]>();

        if (publishAt <= timeProvider.GetUtcNow().UtcDateTime)
            errors["publishAt"] = ["Choose a time in the future."];

        if (check.Entities.Count == 0)
            errors["entries"] = ["Choose at least one change to publish."];

        var taken = check.Entities
            .Select(e => (e.Entity, Publish: check.State.ScheduledPublishOf(e.Entity.Kind, e.Entity.Id)))
            .Where(e => e.Publish is not null && e.Publish.Id != exceptPublishId)
            .Select(e => $"‘{e.Entity.Name}’ is already scheduled for another publish.")
            .ToArray();

        if (taken.Length > 0)
            errors["entries"] = taken;

        return errors;
    }

    /// <summary>One entry per draft in the check: what has no draft has nothing to publish.</summary>
    private static List<ScheduledPublishEntryInsert> Entries(Guid publishId, PublishCheck check) =>
        check.Entities.Select(e => new ScheduledPublishEntryInsert
        {
            ScheduledPublishId = publishId,
            CollectionId = e.Entity.Kind == DraftKind.Collection ? e.Entity.Id : null,
            PageId = e.Entity.Kind == DraftKind.Page ? e.Entity.Id : null,
            CollectionItemId = e.Entity.Kind == DraftKind.Item ? e.Entity.Id : null,
        }).ToList();

    private async Task ReplaceEntriesAsync(Guid publishId, PublishCheck check, CancellationToken cancellationToken)
    {
        var existing = await entryCoreService.GetScheduledPublishEntriesAsync(new ScheduledPublishEntryFilter { ScheduledPublishId = publishId }, cancellationToken);

        foreach (var entry in existing)
            await entryCoreService.DeleteAsync(entry.Id, cancellationToken);

        await entryCoreService.CreateAsync(Entries(publishId, check), cancellationToken);
    }

    /// <summary>Removes the entry, and cancels its publish when nothing is left in it.</summary>
    private async Task RemoveEntryAsync(ScheduledPublishEntry entry, CancellationToken cancellationToken)
    {
        await entryCoreService.DeleteAsync(entry.Id, cancellationToken);

        var left = await entryCoreService.GetScheduledPublishEntriesAsync(new ScheduledPublishEntryFilter { ScheduledPublishId = entry.ScheduledPublishId }, cancellationToken);

        if (left.Count == 0)
            await publishCoreService.UpdateAsync(entry.ScheduledPublishId, new ScheduledPublishUpdate
            {
                Status = ScheduledPublishStatus.Cancelled,
                CompletedAt = timeProvider.GetUtcNow().UtcDateTime,
            }, cancellationToken);
    }

    private async Task<PublishSelection> SelectionOfAsync(ScheduledPublish publish, CancellationToken cancellationToken)
    {
        var entries = await entryCoreService.GetScheduledPublishEntriesAsync(new ScheduledPublishEntryFilter { ScheduledPublishId = publish.Id }, cancellationToken);

        return new PublishSelection(
            publish.WebsiteId,
            entries.Select(e => e.CollectionId).OfType<Guid>().ToList(),
            entries.Select(e => e.PageId).OfType<Guid>().ToList(),
            entries.Select(e => e.CollectionItemId).OfType<Guid>().ToList());
    }

    private async Task<ScheduledPublishDetails> DetailsAsync(ScheduledPublish publish, WebsiteState state, CancellationToken cancellationToken)
    {
        var selection = await SelectionOfAsync(publish, cancellationToken);

        var entries = selection.All()
            .Select(e => Name(state, e.Kind, e.Id))
            .OfType<EntityRef>()
            .ToList();

        return new ScheduledPublishDetails(publish, entries);
    }

    private static EntityRef? Name(WebsiteState state, DraftKind kind, Guid id) => kind switch
    {
        DraftKind.Collection => state.Collection(id)?.Ref(),
        DraftKind.Page => state.Page(id)?.Ref(),
        _ => state.Item(id) is { } item ? item.Ref(state.Collection(item.CollectionId)) : null,
    };
}
