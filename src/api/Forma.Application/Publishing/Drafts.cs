using System.Text.Json;
using Forma.Application.Entities;

namespace Forma.Application.Publishing;

/// <summary>
/// What to put live: drafts of a website's collections, pages and items. The same shape is used by publish now,
/// the check and scheduled publishes.
/// </summary>
public sealed record PublishSelection(
    Guid WebsiteId,
    IReadOnlyCollection<Guid> Collections,
    IReadOnlyCollection<Guid> Pages,
    IReadOnlyCollection<Guid> Items)
{
    public bool Contains(DraftKind kind, Guid id) => kind switch
    {
        DraftKind.Collection => Collections.Contains(id),
        DraftKind.Page => Pages.Contains(id),
        _ => Items.Contains(id),
    };

    public bool IsEmpty => Collections.Count == 0 && Pages.Count == 0 && Items.Count == 0;

    public PublishSelection With(EntityRef entity) => entity.Kind switch
    {
        DraftKind.Collection => this with { Collections = [.. Collections, entity.Id] },
        DraftKind.Page => this with { Pages = [.. Pages, entity.Id] },
        _ => this with { Items = [.. Items, entity.Id] },
    };

    public IEnumerable<(DraftKind Kind, Guid Id)> All() =>
        Collections.Select(id => (DraftKind.Collection, id))
            .Concat(Pages.Select(id => (DraftKind.Page, id)))
            .Concat(Items.Select(id => (DraftKind.Item, id)));
}

/// <summary>A draft, with a one-line summary of what changed, as the top bar and the publish dialog list it.</summary>
/// <param name="Entity">The collection, page or item.</param>
/// <param name="Summary">What changed, e.g. <c>Removed Cover image, added Hero image</c>.</param>
/// <param name="Details">More lines, e.g. <c>14 items: Cover image moves to Hero image</c>.</param>
/// <param name="IsNew">Whether it was never published.</param>
/// <param name="UpdatedAt">When the draft was last saved (UTC).</param>
/// <param name="ScheduledPublish">The pending scheduled publish it is in, if any.</param>
public sealed record DraftSummary(
    EntityRef Entity,
    string Summary,
    IReadOnlyList<string> Details,
    bool IsNew,
    DateTime? UpdatedAt,
    ScheduledPublish? ScheduledPublish);

/// <summary>Names entities and describes their drafts.</summary>
public static class DraftDescriber
{
    public static EntityRef Ref(this Collection collection) => new(DraftKind.Collection, collection.Id, collection.Name);

    public static EntityRef Ref(this Page page) => new(DraftKind.Page, page.Id, page.Title);

    /// <summary>An item is called by its first text field (its title), or its slug when that is empty.</summary>
    public static EntityRef Ref(this CollectionItem item, Collection? collection)
    {
        var fields = collection?.DraftFields ?? collection?.Fields ?? [];
        var values = item.DraftValues ?? item.Values;
        var title = fields
            .Where(f => f.Type == CollectionFieldType.Text)
            .Select(f => values.TryGetValue(f.Id, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        return new(DraftKind.Item, item.Id, title ?? item.DraftSlug ?? item.Slug, item.CollectionId);
    }

    /// <summary>Every draft of the website: collections, then pages, then items.</summary>
    public static IReadOnlyList<DraftSummary> Drafts(WebsiteState state)
    {
        var collections = state.Collections.Where(c => c.HasDraft).Select(c => Describe(state, c));
        var pages = state.Pages
            .Where(p => state.Contents.TryGetValue(p.Id, out var content) && content.HasDraft)
            .Select(p => Describe(state, p));
        var items = state.Items.Where(i => i.HasDraft).Select(i => Describe(state, i));

        return [.. collections, .. pages, .. items];
    }

    public static DraftSummary? Describe(WebsiteState state, DraftKind kind, Guid id) => kind switch
    {
        DraftKind.Collection => state.Collection(id) is { HasDraft: true } c ? Describe(state, c) : null,
        DraftKind.Page => state.Page(id) is { } p && state.Contents.GetValueOrDefault(p.Id)?.HasDraft == true ? Describe(state, p) : null,
        _ => state.Item(id) is { HasDraft: true } i ? Describe(state, i) : null,
    };

    private static DraftSummary Describe(WebsiteState state, Collection collection)
    {
        var scheduled = state.ScheduledPublishOf(DraftKind.Collection, collection.Id);

        if (!collection.IsLive)
            return new(collection.Ref(), "New collection", [], true, collection.DraftUpdatedAt, scheduled);

        var changes = FieldChanges(collection);
        var summary = changes.Count == 0 ? "Edited" : Sentence(changes);

        return new(collection.Ref(), summary, MoveDetails(state, collection), false, collection.DraftUpdatedAt, scheduled);
    }

    private static DraftSummary Describe(WebsiteState state, Page page)
    {
        var content = state.Contents[page.Id];
        var summary = content.IsLive ? "Edited" : "New page";

        return new(page.Ref(), summary, [], !content.IsLive, content.DraftUpdatedAt, state.ScheduledPublishOf(DraftKind.Page, page.Id));
    }

    private static DraftSummary Describe(WebsiteState state, CollectionItem item)
    {
        var collection = state.Collection(item.CollectionId);
        var summary = item.IsLive ? "Edited" : "New";
        var details = collection is null ? [] : new[] { $"In {collection.Name}" };

        return new(item.Ref(collection), summary, details, !item.IsLive, item.DraftUpdatedAt, state.ScheduledPublishOf(DraftKind.Item, item.Id));
    }

    /// <summary><c>Removed Cover image</c>, <c>added Hero image</c>, <c>renamed Title to Headline</c>, <c>address is now /posts</c>.</summary>
    private static List<string> FieldChanges(Collection collection)
    {
        var live = collection.Fields;
        var draft = collection.DraftFields!;
        var changes = new List<string>();

        foreach (var field in live.Where(f => draft.All(d => d.Id != f.Id)))
            changes.Add($"removed {field.Label}");

        foreach (var field in draft.Where(f => live.All(l => l.Id != f.Id)))
            changes.Add($"added {field.Label}");

        foreach (var field in draft)
        {
            var before = live.FirstOrDefault(l => l.Id == field.Id);

            if (before is not null && before.Label != field.Label)
                changes.Add($"renamed {before.Label} to {field.Label}");
            else if (before is not null && before != field)
                changes.Add($"changed {field.Label}");
        }

        if (collection.DraftSlug is { } slug && slug != collection.Slug)
            changes.Add($"address is now /{slug}");

        return changes;
    }

    /// <summary><c>14 items: Cover image moves to Hero image</c>, or <c>3 items lose their Cover image</c>.</summary>
    private static List<string> MoveDetails(WebsiteState state, Collection collection)
    {
        var draft = collection.DraftFields!;
        var moves = collection.DraftValueMoves ?? new Dictionary<string, string?>();
        var items = state.Items.Where(i => i.CollectionId == collection.Id).ToList();
        var details = new List<string>();

        foreach (var removed in collection.Fields.Where(f => draft.All(d => d.Id != f.Id)))
        {
            var count = items.Count(i => HasValue(i.Values, removed.Id) || (i.DraftValues is { } d && HasValue(d, removed.Id)));

            if (count == 0)
                continue;

            var target = moves.GetValueOrDefault(removed.Id) is { } to ? draft.FirstOrDefault(f => f.Id == to) : null;

            details.Add(target is null
                ? $"{Items(count)} lose their {removed.Label}"
                : $"{Items(count)}: {removed.Label} moves to {target.Label}");
        }

        return details;
    }

    internal static bool HasValue(IReadOnlyDictionary<string, JsonElement> values, string fieldId) =>
        values.TryGetValue(fieldId, out var value)
        && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
        && !(value.ValueKind == JsonValueKind.String && value.GetString()!.Trim().Length == 0);

    private static string Items(int count) => count == 1 ? "1 item" : $"{count} items";

    private static string Sentence(List<string> parts)
    {
        var text = string.Join(", ", parts);

        return char.ToUpperInvariant(text[0]) + text[1..];
    }
}
