using Forma.Application.Documents;

namespace Forma.Api.Dtos;

/// <summary>Which version of a page's content to read.</summary>
public enum ContentVersion
{
    /// <summary>What visitors see.</summary>
    Live,

    /// <summary>The draft being edited; the live version when there is no draft.</summary>
    Draft,
}

/// <summary>A page's Design Studio document: its live version or its draft.</summary>
/// <param name="PageId">The page the content belongs to.</param>
/// <param name="Document">The page's sections and blocks, in the <paramref name="Version"/> that was read.</param>
/// <param name="Version">Which version <paramref name="Document"/> is: <c>draft</c> or <c>live</c>.</param>
/// <param name="HasDraft">Whether there are changes that aren't live yet.</param>
/// <param name="DraftUpdatedAt">When the draft was last saved (UTC), or <c>null</c> without a draft.</param>
/// <param name="PublishedAt">When the page was last published (UTC), or <c>null</c> while it never was.</param>
/// <param name="CreatedAt">When the content was first stored (UTC).</param>
/// <param name="UpdatedAt">When the content last changed (UTC).</param>
public record PageContentDto(
    Guid PageId,
    PageDocument Document,
    ContentVersion Version,
    bool HasDraft,
    DateTime? DraftUpdatedAt,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Saves a page's draft: nothing changes on the site until it is published.</summary>
/// <param name="Document">The complete document, as the studio normalised it.</param>
public record SavePageDraftRequest(PageDocument Document);
