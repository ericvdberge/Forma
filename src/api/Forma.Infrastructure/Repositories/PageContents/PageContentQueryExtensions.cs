using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;

namespace Forma.Infrastructure.Repositories.PageContents;

/// <summary>Fluent filters for page content. Each one is a no-op when its value is not set.</summary>
internal static class PageContentQueryExtensions
{
    public static IQueryable<PageContentDao> ApplyFilter(this IQueryable<PageContentDao> query, PageContentFilter filter) =>
        query
            .WithPageId(filter.PageId)
            .WithWebsiteId(filter.WebsiteId)
            .WithIsLive(filter.IsLive)
            .WithHasDraft(filter.HasDraft);

    private static IQueryable<PageContentDao> WithPageId(this IQueryable<PageContentDao> query, Guid? pageId) =>
        query.WhereIf(pageId.HasValue, c => c.PageId == pageId);

    private static IQueryable<PageContentDao> WithWebsiteId(this IQueryable<PageContentDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, c => c.Page!.WebsiteId == websiteId);

    private static IQueryable<PageContentDao> WithIsLive(this IQueryable<PageContentDao> query, bool? isLive) =>
        query.WhereIf(isLive.HasValue, c => (c.PublishedAt != null) == isLive);

    private static IQueryable<PageContentDao> WithHasDraft(this IQueryable<PageContentDao> query, bool? hasDraft) =>
        query.WhereIf(hasDraft.HasValue, c => (c.DraftContent != null) == hasDraft);
}
