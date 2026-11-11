using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Daos;

/// <summary>A page as it is stored in the database.</summary>
public class PageDao
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public PageDaoStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WebsiteDao? Website { get; set; }
}

public enum PageDaoStatus
{
    Draft,
    Published,
}
