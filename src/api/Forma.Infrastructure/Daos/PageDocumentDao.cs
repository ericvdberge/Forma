namespace Forma.Infrastructure.Daos;

/// <summary>
/// One version (draft or published) of a page's Design Studio document as it is stored in the database.
/// The content is opaque JSON here; its model lives in <c>Forma.Application.Documents</c>.
/// </summary>
public class PageDocumentDao
{
    public Guid PageId { get; set; }
    public PageDocumentDaoKind Kind { get; set; }

    /// <summary>The document's <c>version</c>.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>The document as canonical JSON.</summary>
    public required string Content { get; set; }

    /// <summary>SHA-256 of <see cref="Content"/>, lowercase hex.</summary>
    public required string ContentHash { get; set; }

    /// <summary>Draft: increases on every save (optimistic concurrency). Published: the draft revision it was copied from.</summary>
    public int Revision { get; set; }

    public DateTime UpdatedAt { get; set; }

    public PageDao? Page { get; set; }
}

public enum PageDocumentDaoKind
{
    Draft,
    Published,
}
