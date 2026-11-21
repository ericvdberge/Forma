using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Repositories.CollectionItems;
using Forma.Infrastructure.Repositories.Collections;
using Forma.Infrastructure.Repositories.PageContents;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Infrastructure.Repositories.ScheduledPublishEntries;
using Forma.Infrastructure.Repositories.ScheduledPublishes;

namespace Forma.Application.Publishing;

/// <summary>
/// Everything of one website that drafts and publishing look at, live and draft, loaded once. Every read is narrowed by
/// the website, so an id that isn't in here belongs to another website or doesn't exist.
/// </summary>
public sealed class WebsiteState
{
    public required Guid WebsiteId { get; init; }
    public required IReadOnlyList<Page> Pages { get; init; }

    /// <summary>The content of the pages that have any, by page id.</summary>
    public required IReadOnlyDictionary<Guid, PageContent> Contents { get; init; }

    public required IReadOnlyList<Collection> Collections { get; init; }
    public required IReadOnlyList<CollectionItem> Items { get; init; }
    public required IReadOnlySet<Guid> Media { get; init; }

    /// <summary>The pending scheduled publishes and what they put live.</summary>
    public required IReadOnlyList<(ScheduledPublish Publish, IReadOnlyList<ScheduledPublishEntry> Entries)> Scheduled { get; init; }

    public Collection? Collection(Guid id) => Collections.FirstOrDefault(c => c.Id == id);

    public Page? Page(Guid id) => Pages.FirstOrDefault(p => p.Id == id);

    public CollectionItem? Item(Guid id) => Items.FirstOrDefault(i => i.Id == id);

    /// <summary>The pending scheduled publish <paramref name="entity"/> is in, if any.</summary>
    public ScheduledPublish? ScheduledPublishOf(DraftKind kind, Guid id) =>
        Scheduled.FirstOrDefault(s => s.Entries.Any(e => EntryIs(e, kind, id))).Publish;

    private static bool EntryIs(ScheduledPublishEntry entry, DraftKind kind, Guid id) => kind switch
    {
        DraftKind.Collection => entry.CollectionId == id,
        DraftKind.Page => entry.PageId == id,
        _ => entry.CollectionItemId == id,
    };
}

/// <summary>Loads a <see cref="WebsiteState"/> through the core services.</summary>
public interface IWebsiteStateLoader
{
    Task<WebsiteState> LoadAsync(Guid websiteId, CancellationToken cancellationToken = default);
}

public class WebsiteStateLoader(
    IPageCoreService pageCoreService,
    IPageContentCoreService pageContentCoreService,
    ICollectionCoreService collectionCoreService,
    ICollectionItemCoreService itemCoreService,
    IMediaCoreService mediaCoreService,
    IScheduledPublishCoreService scheduledPublishCoreService,
    IScheduledPublishEntryCoreService entryCoreService) : IWebsiteStateLoader
{
    private static readonly ScheduledPublishDaoStatus[] Pending = [ScheduledPublishDaoStatus.Pending];

    public async Task<WebsiteState> LoadAsync(Guid websiteId, CancellationToken cancellationToken = default)
    {
        var pages = await pageCoreService.GetPagesAsync(new PageFilter { WebsiteId = websiteId }, cancellationToken);
        var contents = await pageContentCoreService.GetPageContentsAsync(new PageContentFilter { WebsiteId = websiteId }, cancellationToken);
        var collections = await collectionCoreService.GetCollectionsAsync(new CollectionFilter { WebsiteId = websiteId }, cancellationToken);
        var items = await itemCoreService.GetCollectionItemsAsync(new CollectionItemFilter { WebsiteId = websiteId }, cancellationToken);
        var media = await mediaCoreService.GetMediaItemsAsync(websiteId, cancellationToken);

        var publishes = await scheduledPublishCoreService.GetScheduledPublishesAsync(
            new ScheduledPublishFilter { WebsiteId = websiteId, Statuses = Pending },
            cancellationToken);

        var entries = await entryCoreService.GetScheduledPublishEntriesAsync(
            new ScheduledPublishEntryFilter { WebsiteId = websiteId, PublishStatuses = Pending },
            cancellationToken);

        return new WebsiteState
        {
            WebsiteId = websiteId,
            Pages = pages,
            Contents = contents.ToDictionary(c => c.PageId),
            Collections = collections,
            Items = items,
            Media = media.Select(m => m.Id).ToHashSet(),
            Scheduled = publishes
                .Select(p => (p, (IReadOnlyList<ScheduledPublishEntry>)entries.Where(e => e.ScheduledPublishId == p.Id).ToList()))
                .ToList(),
        };
    }
}
