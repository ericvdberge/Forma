using Forma.Application.Documents;

namespace Forma.Application.Plugins;

/// <summary>
/// The events plugins can subscribe to in their <c>plugin.json</c>. Specification: <c>docs/design/plugins.md</c> § 3.
/// </summary>
/// <remarks>
/// <b>Notifications</b> (<see cref="PageDraftSaved"/>, <see cref="PagePublished"/>) are sent after the change is stored,
/// in the background: a plugin can't slow down or undo them, and its output is ignored.
/// <b>Hooks</b> (<see cref="PageCheck"/>) run during the request and their output is used.
/// </remarks>
public static class PluginEvents
{
    /// <summary>Notification: a page's draft changed (autosave). Not sent for saves that change nothing.</summary>
    public const string PageDraftSaved = "page.draftSaved";

    /// <summary>Notification: a page's draft was published.</summary>
    public const string PagePublished = "page.published";

    /// <summary>Hook: a page is being checked (Publish, or <c>POST /pages/{id}/check</c>); the plugin can add issues.</summary>
    public const string PageCheck = "page.check";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>([PageDraftSaved, PagePublished, PageCheck]);

    public static IReadOnlySet<string> Hooks { get; } = new HashSet<string>([PageCheck]);
}

/// <summary>What a plugin receives: <c>{"event": "...", "id": "...", "occurredAt": "...", "data": {...}}</c>.</summary>
/// <param name="Event">One of <see cref="PluginEvents"/>.</param>
/// <param name="Id">Unique per event, so a plugin can recognise an event it has seen.</param>
/// <param name="OccurredAt">When the change happened (UTC).</param>
/// <param name="Data">The event's payload, one of the records below.</param>
public sealed record PluginEventEnvelope(string Event, Guid Id, DateTime OccurredAt, object Data);

/// <summary>The page an event is about.</summary>
public sealed record PluginPageInfo(Guid Id, Guid WebsiteId, string Title, string Slug);

/// <summary>Data of <see cref="PluginEvents.PageDraftSaved"/>.</summary>
public sealed record PageDraftSavedData(PluginPageInfo Page, int Revision, PageDocument Document);

/// <summary>Data of <see cref="PluginEvents.PagePublished"/>.</summary>
public sealed record PagePublishedData(PluginPageInfo Page, int Revision, PageDocument Document);

/// <summary>Data of <see cref="PluginEvents.PageCheck"/>; the plugin answers with <see cref="PageCheckOutput"/>.</summary>
public sealed record PageCheckData(PluginPageInfo Page, PageDocument Document);

/// <summary>What a <see cref="PluginEvents.PageCheck"/> plugin returns: <c>{"issues": [...]}</c>.</summary>
public sealed record PageCheckOutput(IReadOnlyList<PageCheckOutputIssue> Issues);

/// <summary>An issue a plugin found. Same fields as a built-in content issue; the code is prefixed with the plugin id.</summary>
public sealed record PageCheckOutputIssue(
    string Severity,
    string Code,
    string Message,
    string? SectionId = null,
    string? BlockId = null,
    string? Field = null,
    int? ItemIndex = null);
