using Forma.Application.Documents;

namespace Forma.Application.Entities;

/// <summary>The values new page content is created with.</summary>
public class PageContentInsert
{
    public Guid PageId { get; init; }
    public required PageDocument Document { get; init; }
    public PageContentStatus Status { get; init; } = PageContentStatus.Draft;
}
