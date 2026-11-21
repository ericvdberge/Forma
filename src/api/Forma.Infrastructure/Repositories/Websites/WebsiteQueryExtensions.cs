using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;

namespace Forma.Infrastructure.Repositories.Websites;

/// <summary>Fluent filters for websites. Each one is a no-op when its value is not set.</summary>
internal static class WebsiteQueryExtensions
{
    public static IQueryable<WebsiteDao> ApplyFilter(this IQueryable<WebsiteDao> query, WebsiteFilter filter) =>
        query
            .WithId(filter.Id);

    private static IQueryable<WebsiteDao> WithId(this IQueryable<WebsiteDao> query, Guid? id) =>
        query.WhereIf(id.HasValue, w => w.Id == id);
}
