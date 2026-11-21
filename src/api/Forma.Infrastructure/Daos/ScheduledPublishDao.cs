namespace Forma.Infrastructure.Daos;

/// <summary>Drafts of a website to put live together at a set time.</summary>
public class ScheduledPublishDao
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }

    /// <summary>When to publish (UTC).</summary>
    public DateTime PublishAt { get; set; }

    public ScheduledPublishDaoStatus Status { get; set; }

    /// <summary>Why it didn't go live, as JSON, when <see cref="Status"/> is <see cref="ScheduledPublishDaoStatus.Failed"/>.</summary>
    public string? Issues { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>When it was published, failed or was cancelled.</summary>
    public DateTime? CompletedAt { get; set; }

    public WebsiteDao? Website { get; set; }
}

public enum ScheduledPublishDaoStatus
{
    Pending,
    Published,
    Failed,
    Cancelled,
}
