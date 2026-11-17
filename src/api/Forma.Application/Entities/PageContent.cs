using Forma.Application.Documents;

namespace Forma.Application.Entities;

/// <summary>A page's draft and published document.</summary>
/// <param name="PageId">The page the content belongs to.</param>
/// <param name="Draft">The working draft; <see cref="PageDocument.Empty"/> when nothing was saved yet.</param>
/// <param name="DraftRevision">Increases on every save that changes the draft; 0 when nothing was saved yet.</param>
/// <param name="DraftUpdatedAt">When the draft last changed (UTC); null when nothing was saved yet.</param>
/// <param name="Published">What visitors see; null when the page was never published.</param>
/// <param name="PublishedAt">When the page was last published (UTC); null when never.</param>
/// <param name="HasUnpublishedChanges">True when there is no published version or it differs from the draft.</param>
public sealed record PageContent(
    Guid PageId,
    PageDocument Draft,
    int DraftRevision,
    DateTime? DraftUpdatedAt,
    PageDocument? Published,
    DateTime? PublishedAt,
    bool HasUnpublishedChanges);
