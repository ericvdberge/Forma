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
