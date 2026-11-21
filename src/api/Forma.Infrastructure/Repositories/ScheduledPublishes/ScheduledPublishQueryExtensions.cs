using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Extensions;

namespace Forma.Infrastructure.Repositories.ScheduledPublishes;

/// <summary>Fluent filters for scheduled publishes. Each one is a no-op when its value is not set.</summary>
internal static class ScheduledPublishQueryExtensions
{
    public static IQueryable<ScheduledPublishDao> ApplyFilter(this IQueryable<ScheduledPublishDao> query, ScheduledPublishFilter filter) =>
        query
            .WithId(filter.Id)
            .WithWebsiteId(filter.WebsiteId)
            .WithStatuses(filter.Statuses)
            .WithDueBy(filter.DueBy);

    private static IQueryable<ScheduledPublishDao> WithId(this IQueryable<ScheduledPublishDao> query, Guid? id) =>
        query.WhereIf(id.HasValue, p => p.Id == id);

    private static IQueryable<ScheduledPublishDao> WithWebsiteId(this IQueryable<ScheduledPublishDao> query, Guid? websiteId) =>
        query.WhereIf(websiteId.HasValue, p => p.WebsiteId == websiteId);

    private static IQueryable<ScheduledPublishDao> WithStatuses(this IQueryable<ScheduledPublishDao> query, IReadOnlyCollection<ScheduledPublishDaoStatus>? statuses) =>
        query.WhereIf(statuses is not null, p => statuses!.Contains(p.Status));

    private static IQueryable<ScheduledPublishDao> WithDueBy(this IQueryable<ScheduledPublishDao> query, DateTime? dueBy) =>
        query.WhereIf(dueBy.HasValue, p => p.PublishAt <= dueBy);
}
