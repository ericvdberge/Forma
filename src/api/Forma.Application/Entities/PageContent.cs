using Forma.Application.Documents;

namespace Forma.Application.Entities;

/// <summary>
/// A page's Design Studio documents: the live one visitors see and the draft being edited
/// (<c>docs/design/drafts-and-publish.md</c>). Editing only ever changes the draft.
/// </summary>
public class PageContent
{
    public Guid PageId { get; set; }

    /// <summary>The live document, or <c>null</c> while the page was never published.</summary>
    public PageDocument? Document { get; set; }

    /// <summary>The draft, or <c>null</c> when there are no changes to publish.</summary>
    public PageDocument? DraftDocument { get; set; }

    public DateTime? DraftUpdatedAt { get; set; }

    /// <summary>When the page was last published, or <c>null</c> while it never was.</summary>
    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Whether visitors see the page.</summary>
    public bool IsLive => PublishedAt is not null;

    /// <summary>Whether there are changes to publish.</summary>
    public bool HasDraft => DraftDocument is not null;
}
