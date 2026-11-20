namespace Forma.Infrastructure.Repositories.PageContents;

/// <summary>
/// Criteria for querying page content. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class PageContentFilter
{
    /// <summary>Only the content of this page.</summary>
    public Guid? PageId { get; init; }

    /// <summary>Only content of pages that belong to this website.</summary>
    public Guid? WebsiteId { get; init; }
}
