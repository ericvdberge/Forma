namespace Forma.Infrastructure.Daos;

/// <summary>
/// A website's collection (e.g. <i>Blog posts</i>) as it is stored in the database.
/// Its field definitions are opaque JSON here; their model lives in <c>Forma.Application.Entities</c>.
/// </summary>
public class CollectionDao
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public required string Name { get; set; }
    public required string SingularName { get; set; }
    public required string Slug { get; set; }

    /// <summary>Name of the icon the sidebar shows, e.g. <c>newspaper</c>.</summary>
    public required string Icon { get; set; }

    /// <summary>The field definitions as JSON, in display order.</summary>
    public required string Fields { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WebsiteDao? Website { get; set; }
}
