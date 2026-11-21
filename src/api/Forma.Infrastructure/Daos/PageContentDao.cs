namespace Forma.Infrastructure.Daos;

/// <summary>
/// A page's Design Studio document as it is stored in the database: one row per page, with the live document visitors
/// see and the draft being edited. The content is opaque JSON here; its model lives in <c>Forma.Application.Documents</c>.
/// </summary>
public class PageContentDao
{
    public Guid PageId { get; set; }

    /// <summary>The live document's <c>version</c>.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>The live document as JSON, or <c>null</c> while the page was never published.</summary>
    public string? Content { get; set; }

    /// <summary>The draft document's <c>version</c>, or <c>null</c> when there is no draft.</summary>
    public int? DraftSchemaVersion { get; set; }

    /// <summary>The draft document as JSON, or <c>null</c> when there is no draft.</summary>
    public string? DraftContent { get; set; }

    public DateTime? DraftUpdatedAt { get; set; }

    /// <summary>When the page was last published, or <c>null</c> while it never was.</summary>
    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PageDao? Page { get; set; }
}
