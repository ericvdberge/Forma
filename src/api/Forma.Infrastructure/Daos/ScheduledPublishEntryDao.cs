namespace Forma.Infrastructure.Daos;

/// <summary>One thing a scheduled publish puts live: exactly one of a collection, a page or an item.</summary>
public class ScheduledPublishEntryDao
{
    public Guid Id { get; set; }
    public Guid ScheduledPublishId { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? PageId { get; set; }
    public Guid? CollectionItemId { get; set; }

    public ScheduledPublishDao? ScheduledPublish { get; set; }
    public CollectionDao? Collection { get; set; }
    public PageDao? Page { get; set; }
    public CollectionItemDao? CollectionItem { get; set; }
}
