namespace Forma.Infrastructure.Repositories.PageBindingUsages;

/// <summary>
/// Criteria for querying which collection fields pages show. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class PageBindingUsageFilter
{
    /// <summary>Only the fields this page shows.</summary>
    public Guid? PageId { get; init; }

    /// <summary>Only fields of this collection.</summary>
    public Guid? CollectionId { get; init; }

    /// <summary>Only pages of this website.</summary>
    public Guid? WebsiteId { get; init; }
}
