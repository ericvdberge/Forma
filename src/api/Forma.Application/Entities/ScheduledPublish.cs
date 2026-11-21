namespace Forma.Application.Entities;

/// <summary>Drafts of a website the server puts live together at a set time (<c>docs/design/drafts-and-publish.md</c> § 4.3).</summary>
public class ScheduledPublish
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }

    /// <summary>When to publish (UTC).</summary>
    public DateTime PublishAt { get; set; }

    public ScheduledPublishStatus Status { get; set; }

    /// <summary>Why it didn't go live, when <see cref="Status"/> is <see cref="ScheduledPublishStatus.Failed"/>.</summary>
    public IReadOnlyList<PublishIssue> Issues { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    /// <summary>When it was published, failed or was cancelled.</summary>
    public DateTime? CompletedAt { get; set; }
}

public enum ScheduledPublishStatus
{
    Pending,
    Published,
    Failed,
    Cancelled,
}

/// <summary>One thing a scheduled publish puts live: exactly one of a collection, a page or an item.</summary>
public class ScheduledPublishEntry
{
    public Guid Id { get; set; }
    public Guid ScheduledPublishId { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? PageId { get; set; }
    public Guid? CollectionItemId { get; set; }
}

/// <summary>The values a new scheduled publish is created with.</summary>
public class ScheduledPublishInsert
{
    public Guid WebsiteId { get; init; }
    public DateTime PublishAt { get; init; }
}

/// <summary>Changes to a scheduled publish. Every property is optional: a property that is <c>null</c> is left as it is.</summary>
public class ScheduledPublishUpdate
{
    public DateTime? PublishAt { get; init; }
    public ScheduledPublishStatus? Status { get; init; }
    public IReadOnlyList<PublishIssue>? Issues { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>The values a new entry is created with: exactly one of the ids is set.</summary>
public class ScheduledPublishEntryInsert
{
    public Guid ScheduledPublishId { get; init; }
    public Guid? CollectionId { get; init; }
    public Guid? PageId { get; init; }
    public Guid? CollectionItemId { get; init; }
}
