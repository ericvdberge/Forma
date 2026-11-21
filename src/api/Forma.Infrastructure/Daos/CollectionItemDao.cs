namespace Forma.Infrastructure.Daos;

/// <summary>One item of a collection (e.g. one blog post) as it is stored in the database: live and draft.</summary>
public class CollectionItemDao
{
    public Guid Id { get; set; }
    public Guid CollectionId { get; set; }

    /// <summary>The live slug.</summary>
    public required string Slug { get; set; }

    /// <summary>The live values as a JSON object keyed by field id.</summary>
    public required string Values { get; set; }

    /// <summary>The draft's slug, or <c>null</c> when there is no draft.</summary>
    public string? DraftSlug { get; set; }

    /// <summary>The draft's values as JSON, or <c>null</c> when there is no draft.</summary>
    public string? DraftValues { get; set; }

    public DateTime? DraftUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>When the item was last published, or <c>null</c> while it never was.</summary>
    public DateTime? PublishedAt { get; set; }

    public CollectionDao? Collection { get; set; }
}
