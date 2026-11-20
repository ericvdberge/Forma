namespace Forma.Infrastructure.Daos;

/// <summary>A page as it is stored in the database.</summary>
public class PageDao
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public PageDaoStatus Status { get; set; }
    public PageDaoKind Kind { get; set; }

    /// <summary>The collection whose items a <see cref="PageDaoKind.CollectionTemplate"/> page shows.</summary>
    public Guid? CollectionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WebsiteDao? Website { get; set; }
    public CollectionDao? Collection { get; set; }
}

public enum PageDaoStatus
{
    Draft,
    Published,
}

public enum PageDaoKind
{
    Static,
    CollectionTemplate,
}
