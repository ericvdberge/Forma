namespace Forma.Infrastructure.Daos;

/// <summary>
/// A page's Design Studio document as it is stored in the database: one row per page.
/// The content is opaque JSON here; its model lives in <c>Forma.Application.Documents</c>.
/// </summary>
public class PageContentDao
{
    public Guid PageId { get; set; }
    public PageContentDaoStatus Status { get; set; }

    /// <summary>The document's <c>version</c>.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>The document as JSON.</summary>
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PageDao? Page { get; set; }
}

public enum PageContentDaoStatus
{
    Draft,
    Published,
}
