namespace Forma.Infrastructure.Repositories.Websites;

/// <summary>
/// Criteria for querying websites. Every property is optional: a property that is
/// <c>null</c> is ignored, a property that is set narrows the result.
/// </summary>
public class WebsiteFilter
{
    /// <summary>Only the website with this id.</summary>
    public Guid? Id { get; init; }
}
