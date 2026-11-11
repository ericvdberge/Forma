using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Media;

/// <summary>Fluent filters for media. Each one is a no-op when its value is not set.</summary>
internal static class MediaQueryExtensions
{
    public static IQueryable<MediaItemDao> ApplyFilter(this IQueryable<MediaItemDao> query, MediaFilter filter) =>
        query
            .WithWebsiteId(filter.WebsiteId)
            .WithFileNameContaining(filter.FileName);

    private static IQueryable<MediaItemDao> WithWebsiteId(this IQueryable<MediaItemDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, m => m.WebsiteId == websiteId);

    private static IQueryable<MediaItemDao> WithFileNameContaining(this IQueryable<MediaItemDao> query, string? fileName) =>
        query.WhereIf(!string.IsNullOrWhiteSpace(fileName), m => EF.Functions.Like(m.FileName, $"%{fileName!.Trim()}%"));
}
