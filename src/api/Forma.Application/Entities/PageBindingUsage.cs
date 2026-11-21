namespace Forma.Application.Entities;

/// <summary>A collection field a page's live content shows. Derived from the live document on every publish.</summary>
/// <param name="PageId">The page that shows the field.</param>
/// <param name="CollectionId">The collection the field belongs to.</param>
/// <param name="FieldId">The field, e.g. <c>fld_cover</c>.</param>
/// <param name="Kind">How the page shows it.</param>
public sealed record PageBindingUsage(Guid PageId, Guid CollectionId, string FieldId, PageBindingUsageKind Kind);

public enum PageBindingUsageKind
{
    /// <summary>A block on the collection's detail page is connected to the field.</summary>
    Binding,

    /// <summary>Collection cards show the field.</summary>
    Cards,
}
