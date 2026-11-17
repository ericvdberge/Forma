namespace Forma.Application.Entities;

public class Page
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public PageStatus Status { get; set; } = PageStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum PageStatus
{
    Draft,
    Published,
}
