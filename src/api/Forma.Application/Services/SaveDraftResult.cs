using Forma.Application.Documents.Checks;
using Forma.Application.Entities;

namespace Forma.Application.Services;

/// <summary>Outcome of <see cref="IPageContentCoreService.SaveDraftAsync"/>.</summary>
public abstract record SaveDraftResult
{
    private SaveDraftResult() { }

    /// <summary>
    /// The draft is stored. When the document equals the current draft, nothing is written and
    /// <paramref name="Revision"/> and <paramref name="UpdatedAt"/> are the existing ones.
    /// </summary>
    public sealed record Saved(int Revision, DateTime UpdatedAt, bool HasUnpublishedChanges) : SaveDraftResult;

    public sealed record PageNotFound : SaveDraftResult;

    /// <summary>The base revision is not the current one: the draft was saved elsewhere. <paramref name="Current"/> is the stored state.</summary>
    public sealed record Conflict(PageContent Current) : SaveDraftResult;
}

/// <summary>Outcome of <see cref="IPageContentCoreService.PublishAsync"/>.</summary>
public abstract record PublishResult
{
    private PublishResult() { }

    /// <summary>The draft is live; <paramref name="Content"/> is the page's content now.</summary>
    public sealed record Published(PageContent Content) : PublishResult;

    public sealed record PageNotFound : PublishResult;

    /// <summary>The draft is no longer at the revision that was asked to be published. <paramref name="Current"/> is the stored state.</summary>
    public sealed record Conflict(PageContent Current) : PublishResult;

    /// <summary>The draft has errors; nothing was published. <paramref name="Issues"/> lists all issues, warnings included.</summary>
    public sealed record Rejected(IReadOnlyList<ContentIssue> Issues) : PublishResult;
}

/// <summary>The content issues of a page's draft at <paramref name="DraftRevision"/>.</summary>
public sealed record ContentCheck(int DraftRevision, IReadOnlyList<ContentIssue> Issues);
