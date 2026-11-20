namespace Forma.Application.Entities;

/// <summary>The values a new page is created with.</summary>
public class PageInsert
{
    public Guid WebsiteId { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public PageStatus Status { get; init; } = PageStatus.Draft;
    public PageKind Kind { get; init; } = PageKind.Static;
    public Guid? CollectionId { get; init; }
}
