using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.Pages;

/// <summary>
/// Criteria for querying pages. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class PageFilter
{
    /// <summary>Only the page with this id.</summary>
    public Guid? Id { get; init; }

    /// <summary>Only pages that belong to this website.</summary>
    public Guid? WebsiteId { get; init; }

    /// <summary>Only pages whose title contains this text (case-insensitive).</summary>
    public string? Title { get; init; }

    /// <summary>Only the page with exactly this slug.</summary>
    public string? Slug { get; init; }

    /// <summary>Only pages of this kind.</summary>
    public PageDaoKind? Kind { get; init; }

    /// <summary>Only the detail page of this collection.</summary>
    public Guid? CollectionId { get; init; }
}
