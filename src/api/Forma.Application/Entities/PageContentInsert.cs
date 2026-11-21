using Forma.Application.Documents;

namespace Forma.Application.Entities;

/// <summary>The values new page content is created with. New content usually only has a draft.</summary>
public class PageContentInsert
{
    public Guid PageId { get; init; }
    public PageDocument? Document { get; init; }
    public PageDocument? DraftDocument { get; init; }
    public DateTime? PublishedAt { get; init; }
}
