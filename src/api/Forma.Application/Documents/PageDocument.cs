namespace Forma.Application.Documents;

/// <summary>
/// What the Design Studio edits: a page is a list of sections, each holding a list of blocks.
/// Stored as one JSON value per version (draft, published) and used as is in the API's DTOs,
/// so this model is the contract the app's TypeScript types are generated from.
/// </summary>
/// <remarks>See <c>docs/design/page-content.md</c>.</remarks>
public sealed record PageDocument
{
    /// <summary>The schema version this model reads and writes.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Schema version of the document. Older versions are upgraded on read.</summary>
    public required int Version { get; init; }

    /// <summary>The page's sections, top to bottom.</summary>
    public required IReadOnlyList<Section> Sections { get; init; }

    /// <summary>The document of a page that has no saved content yet.</summary>
    public static PageDocument Empty => new() { Version = CurrentVersion, Sections = [] };
}
