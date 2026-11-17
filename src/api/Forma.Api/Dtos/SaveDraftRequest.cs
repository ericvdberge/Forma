using Forma.Application.Documents;

namespace Forma.Api.Dtos;

/// <summary>A new draft for a page.</summary>
/// <param name="Document">The complete draft document, as the studio normalised it.</param>
/// <param name="BaseRevision">The draft revision this document was edited from (<c>draftRevision</c> from the last load or save); 0 when no draft was saved yet.</param>
public record SaveDraftRequest(PageDocument Document, int BaseRevision);
