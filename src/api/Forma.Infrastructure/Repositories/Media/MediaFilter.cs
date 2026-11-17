namespace Forma.Infrastructure.Repositories.Media;

/// <summary>
/// Criteria for querying media. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class MediaFilter
{
    /// <summary>Only media that belongs to this website.</summary>
    public Guid? WebsiteId { get; init; }

    /// <summary>Only media whose file name contains this text (case-insensitive).</summary>
    public string? FileName { get; init; }
}
