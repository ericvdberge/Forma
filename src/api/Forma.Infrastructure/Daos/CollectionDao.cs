namespace Forma.Infrastructure.Daos;

/// <summary>
/// A website's collection (e.g. <i>Blog posts</i>) as it is stored in the database: the live slug and fields visitors
/// see, and the draft being edited. Field definitions are opaque JSON here; their model lives in <c>Forma.Application.Entities</c>.
/// </summary>
public class CollectionDao
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public required string Name { get; set; }
    public required string SingularName { get; set; }

    /// <summary>The live slug.</summary>
    public required string Slug { get; set; }

    /// <summary>Name of the icon the sidebar shows, e.g. <c>newspaper</c>.</summary>
    public required string Icon { get; set; }

    /// <summary>The live field definitions as JSON, in display order.</summary>
    public required string Fields { get; set; }

    /// <summary>The draft's slug, or <c>null</c> when there is no draft.</summary>
    public string? DraftSlug { get; set; }

    /// <summary>The draft's field definitions as JSON, or <c>null</c> when there is no draft.</summary>
    public string? DraftFields { get; set; }

    /// <summary>Where the values of removed fields go when the draft is published, as JSON (<c>{"fld_old": "fld_new"}</c>).</summary>
    public string? DraftValueMoves { get; set; }

    public DateTime? DraftUpdatedAt { get; set; }

    /// <summary>When the collection was last published, or <c>null</c> while it never was.</summary>
    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WebsiteDao? Website { get; set; }
}
