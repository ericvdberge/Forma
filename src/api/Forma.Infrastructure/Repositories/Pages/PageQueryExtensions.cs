using Forma.Infrastructure.DataAccessObjects.Pages;
using Forma.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Pages;

/// <summary>Fluent filters for pages. Each one is a no-op when its value is not set.</summary>
internal static class PageQueryExtensions
{
    public static IQueryable<PageDao> ApplyFilter(this IQueryable<PageDao> query, PageFilter filter) =>
        query
            .WithWebsiteId(filter.WebsiteId)
            .WithTitleContaining(filter.Title)
            .WithSlug(filter.Slug);

    public static IQueryable<PageDao> WithWebsiteId(this IQueryable<PageDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, p => p.WebsiteId == websiteId);

    public static IQueryable<PageDao> WithTitleContaining(this IQueryable<PageDao> query, string? title) =>
        query.WhereIf(!string.IsNullOrWhiteSpace(title), p => EF.Functions.Like(p.Title, $"%{title!.Trim()}%"));

    public static IQueryable<PageDao> WithSlug(this IQueryable<PageDao> query, string? slug) =>
        query.WhereIf(!string.IsNullOrWhiteSpace(slug), p => p.Slug == slug!.Trim());
}
