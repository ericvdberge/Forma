namespace Forma.Application.Entities;

public class Page
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public PageStatus Status { get; set; } = PageStatus.Draft;

    /// <summary>A page of its own, or the detail page every item of a collection is shown with.</summary>
    public PageKind Kind { get; set; } = PageKind.Static;

    /// <summary>The collection a <see cref="PageKind.CollectionTemplate"/> page shows the items of.</summary>
    public Guid? CollectionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum PageStatus
{
    Draft,
    Published,
}

public enum PageKind
{
    /// <summary>A page of its own, at <c>/{slug}</c>.</summary>
    Static,

    /// <summary>One design for every item of a collection, at <c>/{collection}/{item}</c>.</summary>
    CollectionTemplate,
}
