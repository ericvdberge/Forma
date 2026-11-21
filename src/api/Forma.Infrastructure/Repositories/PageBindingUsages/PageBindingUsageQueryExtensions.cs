using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;

namespace Forma.Infrastructure.Repositories.PageBindingUsages;

/// <summary>Fluent filters for page binding usages. Each one is a no-op when its value is not set.</summary>
internal static class PageBindingUsageQueryExtensions
{
    public static IQueryable<PageBindingUsageDao> ApplyFilter(this IQueryable<PageBindingUsageDao> query, PageBindingUsageFilter filter) =>
        query
            .WithPageId(filter.PageId)
            .WithCollectionId(filter.CollectionId)
            .WithWebsiteId(filter.WebsiteId);

    private static IQueryable<PageBindingUsageDao> WithPageId(this IQueryable<PageBindingUsageDao> query, Guid? pageId) =>
        query.WhereIf(pageId.HasValue, u => u.PageId == pageId);

    private static IQueryable<PageBindingUsageDao> WithCollectionId(this IQueryable<PageBindingUsageDao> query, Guid? collectionId) =>
        query.WhereIf(collectionId.HasValue, u => u.CollectionId == collectionId);

    private static IQueryable<PageBindingUsageDao> WithWebsiteId(this IQueryable<PageBindingUsageDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, u => u.Page!.WebsiteId == websiteId);
}
