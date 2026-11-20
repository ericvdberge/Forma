namespace Forma.Application.Entities;

/// <summary>Changes to a page. Every property is optional: a property that is <c>null</c> is left as it is.</summary>
public class PageUpdate
{
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public PageStatus? Status { get; init; }
}
