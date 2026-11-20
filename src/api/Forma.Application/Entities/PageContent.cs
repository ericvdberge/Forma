using Forma.Application.Documents;

namespace Forma.Application.Entities;

/// <summary>A page's Design Studio document.</summary>
public class PageContent
{
    public Guid PageId { get; set; }
    public required PageDocument Document { get; set; }
    public PageContentStatus Status { get; set; } = PageContentStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum PageContentStatus
{
    Draft,
    Published,
}
