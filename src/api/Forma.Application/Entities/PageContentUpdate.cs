using Forma.Application.Documents;

namespace Forma.Application.Entities;

/// <summary>
/// Changes to a page's content. Every property is optional: a property that is <c>null</c> is left as it is.
/// Publishing is an update that sets <see cref="Status"/> to <see cref="PageContentStatus.Published"/>.
/// </summary>
public class PageContentUpdate
{
    public PageDocument? Document { get; init; }
    public PageContentStatus? Status { get; init; }
}
