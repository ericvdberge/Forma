using Forma.Application.Documents.Validation;

namespace Forma.Application.Entities;

/// <summary>What can go live: a draft of one of these.</summary>
public enum DraftKind
{
    Collection,
    Page,
    Item,
}

/// <summary>A collection, page or item, with the name the app shows it by.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Id">Its id.</param>
/// <param name="Name">What the app calls it: a collection's name, a page's title, an item's title or slug.</param>
/// <param name="CollectionId">The collection of an item.</param>
public sealed record EntityRef(DraftKind Kind, Guid Id, string Name, Guid? CollectionId = null);

/// <summary>
/// A problem found by the publish check, on the entity it is about (<c>null</c> when it is about the publish as a whole). Page issues also point at the section and block,
/// like <see cref="ContentIssue"/>.
/// </summary>
public sealed record PublishIssue(
    IssueSeverity Severity,
    string Code,
    string Message,
    EntityRef? Entity,
    string? SectionId = null,
    string? BlockId = null,
    string? Field = null,
    int? ItemIndex = null);
