using Forma.Application.Documents;

namespace Forma.Application.Entities;

/// <summary>
/// Changes to a page's content. Every property is optional: a property that is <c>null</c> is left as it is.
/// Publishing is an update: <c>new PageContentUpdate { Document = draft, ClearDraft = true, PublishedAt = now }</c>.
/// </summary>
public class PageContentUpdate
{
    /// <summary>The new live document.</summary>
    public PageDocument? Document { get; init; }

    /// <summary>The new draft; saving one also sets <see cref="PageContent.DraftUpdatedAt"/>.</summary>
    public PageDocument? DraftDocument { get; init; }

    /// <summary><c>true</c> removes the draft (after publishing it or discarding it).</summary>
    public bool? ClearDraft { get; init; }

    public DateTime? PublishedAt { get; init; }
}
