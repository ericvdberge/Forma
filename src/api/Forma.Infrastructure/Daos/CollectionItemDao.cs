namespace Forma.Infrastructure.Daos;

/// <summary>One item of a collection (e.g. one blog post) as it is stored in the database.</summary>
public class CollectionItemDao
{
    public Guid Id { get; set; }
    public Guid CollectionId { get; set; }
    public required string Slug { get; set; }
    public CollectionItemDaoStatus Status { get; set; }

    /// <summary>The item's values as a JSON object keyed by field id.</summary>
    public required string Values { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public CollectionDao? Collection { get; set; }
}

public enum CollectionItemDaoStatus
{
    Draft,
    Published,
}
