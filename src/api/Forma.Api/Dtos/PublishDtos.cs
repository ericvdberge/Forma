using Forma.Application.Documents.Validation;
using Forma.Application.Entities;

namespace Forma.Api.Dtos;

/// <summary>A collection, page or item, with the name the app shows it by.</summary>
/// <param name="Kind"><c>collection</c>, <c>page</c> or <c>item</c>.</param>
/// <param name="Id">Its id.</param>
/// <param name="Name">A collection's name, a page's title, or an item's title.</param>
/// <param name="CollectionId">The collection of an item; <c>null</c> otherwise.</param>
public record EntityRefDto(DraftKind Kind, Guid Id, string Name, Guid? CollectionId);

/// <summary>A draft of a website, as the top bar and the publish dialog list it.</summary>
/// <param name="Entity">What the draft is of.</param>
/// <param name="Summary">What changed, e.g. <c>Removed Cover image, added Hero image</c>, <c>Edited</c> or <c>New page</c>.</param>
/// <param name="Details">More lines, e.g. <c>14 items: Cover image moves to Hero image</c>.</param>
/// <param name="IsNew">Whether it was never published.</param>
/// <param name="UpdatedAt">When the draft was last saved (UTC).</param>
/// <param name="ScheduledPublishId">The pending scheduled publish it is in, if any.</param>
/// <param name="ScheduledAt">When that publish runs (UTC).</param>
public record DraftDto(
    EntityRefDto Entity,
    string Summary,
    IReadOnlyList<string> Details,
    bool IsNew,
    DateTime? UpdatedAt,
    Guid? ScheduledPublishId,
    DateTime? ScheduledAt);

/// <summary>What to put live: drafts of one website.</summary>
/// <param name="WebsiteId">The website; every id must be one of its.</param>
/// <param name="Collections">Collections whose draft goes live.</param>
/// <param name="Pages">Pages whose draft goes live.</param>
/// <param name="Items">Items whose draft goes live.</param>
public record PublishRequest(Guid WebsiteId, IReadOnlyList<Guid> Collections, IReadOnlyList<Guid> Pages, IReadOnlyList<Guid> Items);

/// <summary>A problem the publish check found.</summary>
/// <param name="Severity"><c>error</c> blocks publishing; <c>warning</c> doesn't.</param>
/// <param name="Code">Stable code, e.g. <c>binding.field_missing</c> or <c>item.invalid</c>.</param>
/// <param name="Message">What is wrong, in plain language.</param>
/// <param name="Entity">The collection, page or item it is in, to open its editor; <c>null</c> when it is about the publish.</param>
/// <param name="SectionId">On a page: the section it is in.</param>
/// <param name="BlockId">On a page: the block it is about.</param>
/// <param name="Field">The block prop, or on an item the value (<c>values.fld_…</c>).</param>
/// <param name="ItemIndex">The item of a list prop, when it is about one.</param>
public record PublishIssueDto(
    IssueSeverity Severity,
    string Code,
    string Message,
    EntityRefDto? Entity,
    string? SectionId,
    string? BlockId,
    string? Field,
    int? ItemIndex);

/// <summary>A draft that isn't in the publish but is needed to fix it.</summary>
/// <param name="Entity">The draft to add.</param>
/// <param name="NeededBy">What in the publish needs it.</param>
public record PublishNeedDto(EntityRefDto Entity, IReadOnlyList<EntityRefDto> NeededBy);

/// <summary>The result of checking a publish against the site as it would be.</summary>
/// <param name="CanPublish">Whether nothing blocks it.</param>
/// <param name="Entities">The drafts in the publish, with what changed.</param>
/// <param name="Issues">Errors first, then warnings.</param>
/// <param name="Needs">Drafts to add to fix blocking issues; the publish dialog ticks them.</param>
public record PublishCheckDto(bool CanPublish, IReadOnlyList<DraftDto> Entities, IReadOnlyList<PublishIssueDto> Issues, IReadOnlyList<PublishNeedDto> Needs);

/// <summary>What was published.</summary>
/// <param name="PublishedAt">When it went live (UTC).</param>
/// <param name="Entities">What went live.</param>
/// <param name="Warnings">Things worth a look that didn't block publishing.</param>
public record PublishResultDto(DateTime PublishedAt, IReadOnlyList<EntityRefDto> Entities, IReadOnlyList<PublishIssueDto> Warnings);

/// <summary>Drafts put live together at a set time.</summary>
/// <param name="Id">The scheduled publish's id.</param>
/// <param name="WebsiteId">The website.</param>
/// <param name="PublishAt">When it runs (UTC).</param>
/// <param name="Status"><c>pending</c>, or <c>failed</c> when it didn't go live.</param>
/// <param name="Entries">What it puts live.</param>
/// <param name="Issues">Why it didn't go live, when it failed.</param>
/// <param name="CreatedAt">When it was scheduled (UTC).</param>
/// <param name="CompletedAt">When it ran (UTC).</param>
public record ScheduledPublishDto(
    Guid Id,
    Guid WebsiteId,
    DateTime PublishAt,
    ScheduledPublishStatus Status,
    IReadOnlyList<EntityRefDto> Entries,
    IReadOnlyList<PublishIssueDto> Issues,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>Schedules drafts of a website to go live at a time.</summary>
/// <param name="WebsiteId">The website.</param>
/// <param name="PublishAt">When to publish; in the future. Sent with its offset or in UTC.</param>
/// <param name="Collections">Collections whose draft goes live.</param>
/// <param name="Pages">Pages whose draft goes live.</param>
/// <param name="Items">Items whose draft goes live.</param>
public record SchedulePublishRequest(
    Guid WebsiteId,
    DateTimeOffset PublishAt,
    IReadOnlyList<Guid> Collections,
    IReadOnlyList<Guid> Pages,
    IReadOnlyList<Guid> Items);

/// <summary>Changes a scheduled publish: its time, and what is in it when the lists are given.</summary>
/// <param name="PublishAt">The new time, or <c>null</c> to keep it.</param>
/// <param name="Collections">The collections, or <c>null</c> to keep what is in it (all three lists go together).</param>
/// <param name="Pages">The pages.</param>
/// <param name="Items">The items.</param>
public record UpdateScheduledPublishRequest(
    DateTimeOffset? PublishAt,
    IReadOnlyList<Guid>? Collections,
    IReadOnlyList<Guid>? Pages,
    IReadOnlyList<Guid>? Items);
