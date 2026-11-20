using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Pages;

/// <summary>Fluent filters for pages. Each one is a no-op when its value is not set.</summary>
internal static class PageQueryExtensions
{
    public static IQueryable<PageDao> ApplyFilter(this IQueryable<PageDao> query, PageFilter filter) =>
        query
            .WithId(filter.Id)
            .WithWebsiteId(filter.WebsiteId)
            .WithTitleContaining(filter.Title)
            .WithSlug(filter.Slug)
            .WithKind(filter.Kind)
            .WithCollectionId(filter.CollectionId);

    private static IQueryable<PageDao> WithId(this IQueryable<PageDao> query, Guid? id) =>
        query.WhereIf(id.HasValue, p => p.Id == id);

    private static IQueryable<PageDao> WithWebsiteId(this IQueryable<PageDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, p => p.WebsiteId == websiteId);

    private static IQueryable<PageDao> WithTitleContaining(this IQueryable<PageDao> query, string? title) =>
        query.WhereIf(!string.IsNullOrWhiteSpace(title), p => EF.Functions.Like(p.Title, $"%{title!.Trim()}%"));

    private static IQueryable<PageDao> WithSlug(this IQueryable<PageDao> query, string? slug) =>
        query.WhereIf(!string.IsNullOrWhiteSpace(slug), p => p.Slug == slug!.Trim());

    private static IQueryable<PageDao> WithKind(this IQueryable<PageDao> query, PageDaoKind? kind) =>
        query.WhereIf(kind.HasValue, p => p.Kind == kind);

    private static IQueryable<PageDao> WithCollectionId(this IQueryable<PageDao> query, Guid? collectionId) =>
        query.WhereIf(collectionId.HasValue, p => p.CollectionId == collectionId);
}
