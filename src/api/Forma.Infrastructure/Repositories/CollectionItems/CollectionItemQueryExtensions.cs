using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;

namespace Forma.Infrastructure.Repositories.CollectionItems;

/// <summary>Fluent filters for collection items. Each one is a no-op when its value is not set.</summary>
internal static class CollectionItemQueryExtensions
{
    public static IQueryable<CollectionItemDao> ApplyFilter(this IQueryable<CollectionItemDao> query, CollectionItemFilter filter) =>
        query
            .WithId(filter.Id)
            .WithCollectionId(filter.CollectionId)
            .WithWebsiteId(filter.WebsiteId)
            .WithSlug(filter.Slug)
            .WithIsLive(filter.IsLive)
            .WithHasDraft(filter.HasDraft);

    private static IQueryable<CollectionItemDao> WithId(this IQueryable<CollectionItemDao> query, Guid? id) =>
        query.WhereIf(id.HasValue, i => i.Id == id);

    private static IQueryable<CollectionItemDao> WithCollectionId(this IQueryable<CollectionItemDao> query, Guid? collectionId) =>
        query.WhereIf(collectionId.HasValue, i => i.CollectionId == collectionId);

    private static IQueryable<CollectionItemDao> WithWebsiteId(this IQueryable<CollectionItemDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, i => i.Collection!.WebsiteId == websiteId);

    private static IQueryable<CollectionItemDao> WithSlug(this IQueryable<CollectionItemDao> query, string? slug) =>
        query.WhereIf(!string.IsNullOrWhiteSpace(slug), i => i.Slug == slug!.Trim());

    private static IQueryable<CollectionItemDao> WithIsLive(this IQueryable<CollectionItemDao> query, bool? isLive) =>
        query.WhereIf(isLive.HasValue, i => (i.PublishedAt != null) == isLive);

    private static IQueryable<CollectionItemDao> WithHasDraft(this IQueryable<CollectionItemDao> query, bool? hasDraft) =>
        query.WhereIf(hasDraft.HasValue, i => (i.DraftValues != null) == hasDraft);
}
