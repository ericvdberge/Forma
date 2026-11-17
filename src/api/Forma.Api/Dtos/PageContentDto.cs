using Forma.Application.Documents;

namespace Forma.Api.Dtos;

/// <summary>A page's draft and published Design Studio document.</summary>
/// <param name="PageId">The page the content belongs to.</param>
/// <param name="Draft">The working draft. A page without saved content gets an empty document (no sections).</param>
/// <param name="DraftRevision">The draft's revision: send it back as <c>baseRevision</c> when saving. 0 when nothing was saved yet.</param>
/// <param name="DraftUpdatedAt">When the draft last changed (UTC); null when nothing was saved yet.</param>
/// <param name="Published">What visitors see; null when the page was never published.</param>
/// <param name="PublishedAt">When the page was last published (UTC); null when never.</param>
/// <param name="HasUnpublishedChanges">True when there is no published version or it differs from the draft.</param>
public record PageContentDto(
    Guid PageId,
    PageDocument Draft,
    int DraftRevision,
    DateTime? DraftUpdatedAt,
    PageDocument? Published,
    DateTime? PublishedAt,
    bool HasUnpublishedChanges);
