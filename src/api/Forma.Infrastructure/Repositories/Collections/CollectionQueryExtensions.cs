using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;

namespace Forma.Infrastructure.Repositories.Collections;

/// <summary>Fluent filters for collections. Each one is a no-op when its value is not set.</summary>
internal static class CollectionQueryExtensions
{
    public static IQueryable<CollectionDao> ApplyFilter(this IQueryable<CollectionDao> query, CollectionFilter filter) =>
        query
            .WithId(filter.Id)
            .WithWebsiteId(filter.WebsiteId)
            .WithSlug(filter.Slug)
            .WithIsLive(filter.IsLive)
            .WithHasDraft(filter.HasDraft);

    private static IQueryable<CollectionDao> WithId(this IQueryable<CollectionDao> query, Guid? id) =>
        query.WhereIf(id.HasValue, c => c.Id == id);

    private static IQueryable<CollectionDao> WithWebsiteId(this IQueryable<CollectionDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, c => c.WebsiteId == websiteId);

    private static IQueryable<CollectionDao> WithSlug(this IQueryable<CollectionDao> query, string? slug) =>
        query.WhereIf(!string.IsNullOrWhiteSpace(slug), c => c.Slug == slug!.Trim());

    private static IQueryable<CollectionDao> WithIsLive(this IQueryable<CollectionDao> query, bool? isLive) =>
        query.WhereIf(isLive.HasValue, c => (c.PublishedAt != null) == isLive);

    private static IQueryable<CollectionDao> WithHasDraft(this IQueryable<CollectionDao> query, bool? hasDraft) =>
        query.WhereIf(hasDraft.HasValue, c => (c.DraftFields != null) == hasDraft);
}
