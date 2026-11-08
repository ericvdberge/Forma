namespace Forma.Application.Repositories.Pages;

/// <summary>
/// Criteria for querying pages. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class PageFilter
{
    /// <summary>Only pages that belong to this website.</summary>
    public Guid? WebsiteId { get; init; }

    /// <summary>Only pages whose title contains this text (case-insensitive).</summary>
    public string? Title { get; init; }

    /// <summary>Only the page with exactly this slug.</summary>
    public string? Slug { get; init; }
}
