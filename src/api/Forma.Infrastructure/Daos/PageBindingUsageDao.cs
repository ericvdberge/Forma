namespace Forma.Infrastructure.Daos;

/// <summary>
/// A collection field a page's live content shows: a block connected to it, or Collection cards that list it.
/// Rebuilt from the live document every time the page is published.
/// </summary>
public class PageBindingUsageDao
{
    public Guid PageId { get; set; }
    public Guid CollectionId { get; set; }
    public required string FieldId { get; set; }
    public PageBindingUsageDaoKind Kind { get; set; }

    public PageDao? Page { get; set; }
    public CollectionDao? Collection { get; set; }
}

public enum PageBindingUsageDaoKind
{
    /// <summary>A block on a detail page connected to the field.</summary>
    Binding,

    /// <summary>Collection cards that show the field.</summary>
    Cards,
}
