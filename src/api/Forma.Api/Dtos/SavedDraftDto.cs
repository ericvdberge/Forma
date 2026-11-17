namespace Forma.Api.Dtos;

/// <summary>The result of saving a draft.</summary>
/// <param name="Revision">The draft's revision now: send it as <c>baseRevision</c> with the next save. Unchanged when the document was identical.</param>
/// <param name="UpdatedAt">When the draft last changed (UTC).</param>
/// <param name="HasUnpublishedChanges">True when there is no published version or it differs from the draft.</param>
public record SavedDraftDto(int Revision, DateTime UpdatedAt, bool HasUnpublishedChanges);
