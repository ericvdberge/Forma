using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;

namespace Forma.Infrastructure.Repositories.ScheduledPublishEntries;

/// <summary>Fluent filters for scheduled publish entries. Each one is a no-op when its value is not set.</summary>
internal static class ScheduledPublishEntryQueryExtensions
{
    public static IQueryable<ScheduledPublishEntryDao> ApplyFilter(this IQueryable<ScheduledPublishEntryDao> query, ScheduledPublishEntryFilter filter) =>
        query
            .WithId(filter.Id)
            .WithScheduledPublishId(filter.ScheduledPublishId)
            .WithWebsiteId(filter.WebsiteId)
            .WithPublishStatuses(filter.PublishStatuses)
            .WithCollectionId(filter.CollectionId)
            .WithPageId(filter.PageId)
            .WithCollectionItemId(filter.CollectionItemId);

    private static IQueryable<ScheduledPublishEntryDao> WithId(this IQueryable<ScheduledPublishEntryDao> query, Guid? id) =>
        query.WhereIf(id.HasValue, e => e.Id == id);

    private static IQueryable<ScheduledPublishEntryDao> WithScheduledPublishId(this IQueryable<ScheduledPublishEntryDao> query, Guid? publishId) =>
        query.WhereIf(publishId.HasValue, e => e.ScheduledPublishId == publishId);

    private static IQueryable<ScheduledPublishEntryDao> WithWebsiteId(this IQueryable<ScheduledPublishEntryDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, e => e.ScheduledPublish!.WebsiteId == websiteId);

    private static IQueryable<ScheduledPublishEntryDao> WithPublishStatuses(this IQueryable<ScheduledPublishEntryDao> query, IReadOnlyCollection<ScheduledPublishDaoStatus>? statuses) =>
        query.WhereIf(statuses is not null, e => statuses!.Contains(e.ScheduledPublish!.Status));

    private static IQueryable<ScheduledPublishEntryDao> WithCollectionId(this IQueryable<ScheduledPublishEntryDao> query, Guid? collectionId) =>
        query.WhereIf(collectionId.HasValue, e => e.CollectionId == collectionId);

    private static IQueryable<ScheduledPublishEntryDao> WithPageId(this IQueryable<ScheduledPublishEntryDao> query, Guid? pageId) =>
        query.WhereIf(pageId.HasValue, e => e.PageId == pageId);

    private static IQueryable<ScheduledPublishEntryDao> WithCollectionItemId(this IQueryable<ScheduledPublishEntryDao> query, Guid? itemId) =>
        query.WhereIf(itemId.HasValue, e => e.CollectionItemId == itemId);
}
