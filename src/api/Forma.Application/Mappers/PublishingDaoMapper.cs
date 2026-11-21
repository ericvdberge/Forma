using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Infrastructure.Daos;

namespace Forma.Application.Mappers;

/// <summary>Maps page binding usages and scheduled publishes to and from their stored form.</summary>
public static class PublishingDaoMapper
{
    public static PageBindingUsage ToEntity(this PageBindingUsageDao dao) =>
        new(dao.PageId, dao.CollectionId, dao.FieldId, dao.Kind == PageBindingUsageDaoKind.Cards ? PageBindingUsageKind.Cards : PageBindingUsageKind.Binding);

    public static IReadOnlyList<PageBindingUsage> ToEntities(this IReadOnlyList<PageBindingUsageDao> daos) =>
        daos.Select(ToEntity).ToList();

    public static PageBindingUsageDao ToDao(this PageBindingUsage usage) => new()
    {
        PageId = usage.PageId,
        CollectionId = usage.CollectionId,
        FieldId = usage.FieldId,
        Kind = usage.Kind == PageBindingUsageKind.Cards ? PageBindingUsageDaoKind.Cards : PageBindingUsageDaoKind.Binding,
    };

    public static ScheduledPublish ToEntity(this ScheduledPublishDao dao) => new()
    {
        Id = dao.Id,
        WebsiteId = dao.WebsiteId,
        PublishAt = dao.PublishAt,
        Status = Enum.Parse<ScheduledPublishStatus>(dao.Status.ToString()),
        Issues = dao.Issues is null ? [] : JsonSerializer.Deserialize<List<PublishIssue>>(dao.Issues, PageDocumentJson.Options) ?? [],
        CreatedAt = dao.CreatedAt,
        CompletedAt = dao.CompletedAt,
    };

    public static IReadOnlyList<ScheduledPublish> ToEntities(this IReadOnlyList<ScheduledPublishDao> daos) =>
        daos.Select(ToEntity).ToList();

    public static ScheduledPublishDao ToDao(this ScheduledPublish publish) => new()
    {
        Id = publish.Id,
        WebsiteId = publish.WebsiteId,
        PublishAt = publish.PublishAt,
        Status = publish.Status.ToDao(),
        Issues = publish.Issues.Count == 0 ? null : JsonSerializer.Serialize(publish.Issues, PageDocumentJson.Options),
        CreatedAt = publish.CreatedAt,
        CompletedAt = publish.CompletedAt,
    };

    public static ScheduledPublishDaoStatus ToDao(this ScheduledPublishStatus status) =>
        Enum.Parse<ScheduledPublishDaoStatus>(status.ToString());

    public static ScheduledPublishEntry ToEntity(this ScheduledPublishEntryDao dao) => new()
    {
        Id = dao.Id,
        ScheduledPublishId = dao.ScheduledPublishId,
        CollectionId = dao.CollectionId,
        PageId = dao.PageId,
        CollectionItemId = dao.CollectionItemId,
    };

    public static IReadOnlyList<ScheduledPublishEntry> ToEntities(this IReadOnlyList<ScheduledPublishEntryDao> daos) =>
        daos.Select(ToEntity).ToList();

    public static ScheduledPublishEntryDao ToDao(this ScheduledPublishEntry entry) => new()
    {
        Id = entry.Id,
        ScheduledPublishId = entry.ScheduledPublishId,
        CollectionId = entry.CollectionId,
        PageId = entry.PageId,
        CollectionItemId = entry.CollectionItemId,
    };
}
