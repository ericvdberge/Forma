using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Forma.Infrastructure.Repositories.CollectionItems;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Infrastructure.Repositories.ScheduledPublishEntries;

namespace Forma.Application.Publishing;

/// <summary>Outcome of <see cref="IPublishService.PublishAsync"/>.</summary>
public abstract record PublishOutcome
{
    private PublishOutcome() { }

    /// <summary>Everything in the publish is live.</summary>
    public sealed record Published(PublishCheck Check, DateTime PublishedAt) : PublishOutcome;

    /// <summary>The check found errors; nothing was published.</summary>
    public sealed record Blocked(PublishCheck Check) : PublishOutcome;

    /// <summary>An id isn't one of the website's.</summary>
    public sealed record NotFound : PublishOutcome;
}

/// <summary>
/// Puts drafts live in one transaction (<c>docs/design/drafts-and-publish.md</c> § 6): checks the publish, publishes the
/// collections (moving item values), then the items and pages, rebuilds the pages' binding usages and takes what was
/// published out of pending scheduled publishes. Any failure rolls all of it back.
/// </summary>
public interface IPublishService
{
    /// <param name="selection">What to put live.</param>
    /// <param name="runningPublishId">The scheduled publish being run, which keeps its entries.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    Task<PublishOutcome> PublishAsync(PublishSelection selection, Guid? runningPublishId = null, CancellationToken cancellationToken = default);
}

public class PublishService(
    ITransactionRunner transactionRunner,
    IPublishCheckService checkService,
    ICollectionCoreService collectionCoreService,
    ICollectionItemCoreService itemCoreService,
    IPageCoreService pageCoreService,
    IPageContentCoreService pageContentCoreService,
    IPageBindingUsageService usageService,
    IScheduledPublishEntryCoreService entryCoreService,
    IScheduledPublishCoreService scheduledPublishCoreService,
    TimeProvider timeProvider) : IPublishService
{
    private static readonly ScheduledPublishDaoStatus[] Pending = [ScheduledPublishDaoStatus.Pending];

    public Task<PublishOutcome> PublishAsync(PublishSelection selection, Guid? runningPublishId = null, CancellationToken cancellationToken = default) =>
        transactionRunner.RunAsync(
            ct => PublishInTransactionAsync(selection, runningPublishId, ct),
            commit: outcome => outcome is PublishOutcome.Published,
            cancellationToken);

    private async Task<PublishOutcome> PublishInTransactionAsync(PublishSelection selection, Guid? runningPublishId, CancellationToken cancellationToken)
    {
        var check = await checkService.CheckAsync(selection, cancellationToken);

        if (check is null)
            return new PublishOutcome.NotFound();

        if (!check.CanPublish)
            return new PublishOutcome.Blocked(check);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var state = check.State;

        foreach (var collection in selection.Collections.Select(state.Collection).OfType<Collection>().Where(c => c.HasDraft))
            await PublishCollectionAsync(collection, now, cancellationToken);

        foreach (var id in selection.Items)
            await PublishItemAsync(id, now, cancellationToken);

        foreach (var page in selection.Pages.Select(state.Page).OfType<Page>())
            await PublishPageAsync(page, now, cancellationToken);

        await RemoveFromScheduledPublishesAsync(selection, runningPublishId, now, cancellationToken);

        return new PublishOutcome.Published(check, now);
    }

    /// <summary>Copies the draft's slug and fields to live, moves the items' values, and moves the detail page's address.</summary>
    private async Task PublishCollectionAsync(Collection collection, DateTime now, CancellationToken cancellationToken)
    {
        await collectionCoreService.UpdateAsync(collection.Id, new CollectionUpdate
        {
            Slug = collection.DraftSlug,
            Fields = collection.DraftFields,
            ClearDraft = true,
            PublishedAt = now,
        }, cancellationToken);

        await MoveItemValuesAsync(collection, cancellationToken);

        if (collection.DraftSlug is { } slug && slug != collection.Slug)
            await MoveDetailPageAsync(collection, slug, cancellationToken);
    }

    /// <summary>Rewrites every item's live values and draft with the collection's value moves.</summary>
    private async Task MoveItemValuesAsync(Collection collection, CancellationToken cancellationToken)
    {
        var items = await itemCoreService.GetCollectionItemsAsync(new CollectionItemFilter { CollectionId = collection.Id }, cancellationToken);

        foreach (var item in items)
        {
            var values = ValueMoves.Apply(collection, item.Values);
            var draft = item.DraftValues is null ? null : ValueMoves.Apply(collection, item.DraftValues);

            var valuesChange = ValueMoves.Changes(item.Values, values);
            var draftChanges = item.DraftValues is not null && ValueMoves.Changes(item.DraftValues, draft!);

            if (!valuesChange && !draftChanges)
                continue;

            await itemCoreService.UpdateAsync(item.Id, new CollectionItemUpdate
            {
                Values = valuesChange ? values : null,
                DraftValues = draftChanges ? draft : null,
            }, cancellationToken);
        }
    }

    private async Task MoveDetailPageAsync(Collection collection, string slug, CancellationToken cancellationToken)
    {
        var detailPage = await pageCoreService.GetPageAsync(
            new PageFilter { WebsiteId = collection.WebsiteId, CollectionId = collection.Id, Kind = PageDaoKind.CollectionTemplate },
            cancellationToken);

        if (detailPage is not null)
            await pageCoreService.UpdateAsync(detailPage.Id, new PageUpdate { Slug = $"{slug}/{{item}}" }, cancellationToken);
    }

    /// <summary>Copies the item's draft to live. Read again, because publishing its collection may have moved its values.</summary>
    private async Task PublishItemAsync(Guid id, DateTime now, CancellationToken cancellationToken)
    {
        var item = await itemCoreService.GetCollectionItemAsync(new CollectionItemFilter { Id = id }, cancellationToken);

        if (item is not { HasDraft: true })
            return;

        await itemCoreService.UpdateAsync(item.Id, new CollectionItemUpdate
        {
            Slug = item.DraftSlug,
            Values = item.DraftValues,
            ClearDraft = true,
            PublishedAt = now,
        }, cancellationToken);
    }

    /// <summary>Copies the page's draft to live and records which collection fields it now shows.</summary>
    private async Task PublishPageAsync(Page page, DateTime now, CancellationToken cancellationToken)
    {
        var content = await pageContentCoreService.GetPageContentAsync(new Infrastructure.Repositories.PageContents.PageContentFilter { PageId = page.Id }, cancellationToken);

        if (content?.DraftDocument is not { } draft)
            return;

        await pageContentCoreService.UpdateAsync(page.Id, new PageContentUpdate
        {
            Document = draft,
            ClearDraft = true,
            PublishedAt = now,
        }, cancellationToken);

        if (page.Status != PageStatus.Published)
            await pageCoreService.UpdateAsync(page.Id, new PageUpdate { Status = PageStatus.Published }, cancellationToken);

        await usageService.RebuildAsync(page, draft, cancellationToken);
    }

    /// <summary>
    /// Takes what was published out of pending scheduled publishes: it can't go live twice. A scheduled publish left
    /// with nothing in it is cancelled.
    /// </summary>
    private async Task RemoveFromScheduledPublishesAsync(PublishSelection selection, Guid? runningPublishId, DateTime now, CancellationToken cancellationToken)
    {
        var entries = await entryCoreService.GetScheduledPublishEntriesAsync(
            new ScheduledPublishEntryFilter { WebsiteId = selection.WebsiteId, PublishStatuses = Pending },
            cancellationToken);

        var published = entries
            .Where(e => e.ScheduledPublishId != runningPublishId)
            .Where(e => Contains(selection, e))
            .ToList();

        foreach (var entry in published)
            await entryCoreService.DeleteAsync(entry.Id, cancellationToken);

        var emptied = published
            .Select(e => e.ScheduledPublishId)
            .Distinct()
            .Where(id => entries.Where(e => e.ScheduledPublishId == id).All(published.Contains));

        foreach (var id in emptied)
            await scheduledPublishCoreService.UpdateAsync(id, new ScheduledPublishUpdate { Status = ScheduledPublishStatus.Cancelled, CompletedAt = now }, cancellationToken);
    }

    internal static bool Contains(PublishSelection selection, ScheduledPublishEntry entry) =>
        (entry.CollectionId is { } c && selection.Collections.Contains(c))
        || (entry.PageId is { } p && selection.Pages.Contains(p))
        || (entry.CollectionItemId is { } i && selection.Items.Contains(i));
}
