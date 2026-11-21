using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Application.Services;

namespace Forma.Application.Publishing;

/// <summary>A draft that isn't in the publish but would fix a blocking issue, and what in the publish needs it.</summary>
public sealed record PublishNeed(EntityRef Entity, IReadOnlyList<EntityRef> NeededBy);

/// <summary>The result of checking a publish (<c>docs/design/drafts-and-publish.md</c> § 6).</summary>
/// <param name="Selection">What was checked.</param>
/// <param name="Entities">A summary of each draft in the publish.</param>
/// <param name="Issues">What is wrong with the site as it would be: errors block, warnings don't. Errors first.</param>
/// <param name="Needs">Drafts not in the publish that would fix blocking issues.</param>
/// <param name="State">The website as it was checked.</param>
public sealed record PublishCheck(
    PublishSelection Selection,
    IReadOnlyList<DraftSummary> Entities,
    IReadOnlyList<PublishIssue> Issues,
    IReadOnlyList<PublishNeed> Needs,
    WebsiteState State)
{
    public bool CanPublish => Issues.All(i => i.Severity != IssueSeverity.Error);
}

/// <summary>
/// Checks a publish: builds the site as it would be (the drafts being published, everything else as it is live) and runs
/// the existing rules on it: item values against their collection's fields, page documents with their bindings and
/// Collection cards, unique slugs, and every live page that uses a collection in the publish.
/// </summary>
public interface IPublishCheckService
{
    /// <summary>The check, or <c>null</c> when an id in <paramref name="selection"/> isn't one of the website's.</summary>
    Task<PublishCheck?> CheckAsync(PublishSelection selection, CancellationToken cancellationToken = default);
}

public class PublishCheckService(
    IWebsiteStateLoader stateLoader,
    ICollectionItemValidationService itemValidationService,
    IPageContentValidationService pageValidationService) : IPublishCheckService
{
    /// <summary>How many rounds of "this draft needs that one" are followed.</summary>
    private const int MaxNeedRounds = 4;

    public async Task<PublishCheck?> CheckAsync(PublishSelection selection, CancellationToken cancellationToken = default)
    {
        var state = await stateLoader.LoadAsync(selection.WebsiteId, cancellationToken);

        if (!BelongsToWebsite(state, selection))
            return null;

        var issues = Evaluate(state, selection);
        var needs = FindNeeds(state, selection, issues);

        var entities = selection.All()
            .Select(e => DraftDescriber.Describe(state, e.Kind, e.Id))
            .OfType<DraftSummary>()
            .ToList();

        return new PublishCheck(selection, entities, Sorted(issues), needs, state);
    }

    /// <summary>Every id was read narrowed by the website, so one that isn't in the state is another website's (IDOR).</summary>
    private static bool BelongsToWebsite(WebsiteState state, PublishSelection selection) =>
        selection.Collections.All(id => state.Collection(id) is not null)
        && selection.Pages.All(id => state.Page(id) is not null)
        && selection.Items.All(id => state.Item(id) is not null);

    private static List<PublishIssue> Sorted(List<PublishIssue> issues) =>
        issues.OrderBy(i => i.Severity).ToList();

    /* ---------- The site as it would be ---------- */

    private List<PublishIssue> Evaluate(WebsiteState state, PublishSelection selection)
    {
        var issues = new List<PublishIssue>();

        var changed = state.Collections
            .Where(c => c.HasDraft && selection.Collections.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSet();

        var collections = ViewCollections(state, changed);
        var items = ViewItems(state, selection, collections, changed, issues);
        var site = new SiteSnapshot(state.Media, state.Pages, collections.Values.ToList(), items.Select(i => i.View).ToList());

        CheckItems(state, collections, items, issues);
        CheckItemSlugs(collections, items, issues);
        CheckCollectionSlugs(state, collections, changed, issues);
        CheckPages(state, selection, site, changed, issues);

        return issues;
    }

    /// <summary>The live collections as visitors would see them: the draft's slug and fields for those being published.</summary>
    private static Dictionary<Guid, Collection> ViewCollections(WebsiteState state, HashSet<Guid> changed)
    {
        var view = new Dictionary<Guid, Collection>();

        foreach (var collection in state.Collections)
        {
            if (changed.Contains(collection.Id))
                view[collection.Id] = AsPublished(collection);
            else if (collection.IsLive)
                view[collection.Id] = collection;
        }

        return view;
    }

    private static Collection AsPublished(Collection collection) => new()
    {
        Id = collection.Id,
        WebsiteId = collection.WebsiteId,
        Name = collection.Name,
        SingularName = collection.SingularName,
        Slug = collection.DraftSlug ?? collection.Slug,
        Icon = collection.Icon,
        Fields = collection.DraftFields ?? collection.Fields,
        PublishedAt = collection.PublishedAt ?? DateTime.UtcNow,
        CreatedAt = collection.CreatedAt,
        UpdatedAt = collection.UpdatedAt,
    };

    /// <param name="Source">The item as it is stored.</param>
    /// <param name="View">The item as visitors would see it.</param>
    /// <param name="Checked">Whether its values are checked: it is being published, or its collection is.</param>
    private sealed record ItemView(CollectionItem Source, CollectionItem View, bool Checked, bool InPublish);

    /// <summary>The live items as visitors would see them, with the values moved for collections being published.</summary>
    private static List<ItemView> ViewItems(
        WebsiteState state,
        PublishSelection selection,
        Dictionary<Guid, Collection> collections,
        HashSet<Guid> changed,
        List<PublishIssue> issues)
    {
        var view = new List<ItemView>();

        foreach (var item in state.Items)
        {
            var inPublish = item.HasDraft && selection.Items.Contains(item.Id);

            if (!inPublish && !item.IsLive)
                continue;

            var source = state.Collection(item.CollectionId)!;

            if (!collections.ContainsKey(item.CollectionId))
            {
                if (inPublish)
                    issues.Add(Error("item.collection_not_live", $"{source.Name} isn't live yet, so its items can't be either.", item.Ref(source)));

                continue;
            }

            var values = inPublish ? item.DraftValues! : item.Values;

            if (changed.Contains(item.CollectionId))
                values = ValueMoves.Apply(source, values);

            var shown = new CollectionItem
            {
                Id = item.Id,
                CollectionId = item.CollectionId,
                Slug = inPublish ? item.DraftSlug ?? item.Slug : item.Slug,
                Values = values,
                PublishedAt = item.PublishedAt ?? DateTime.UtcNow,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
            };

            view.Add(new ItemView(item, shown, inPublish || changed.Contains(item.CollectionId), inPublish));
        }

        return view;
    }

    /* ---------- Rules ---------- */

    /// <summary>Item values against their collection's fields, with the same rules as saving an item.</summary>
    private void CheckItems(WebsiteState state, Dictionary<Guid, Collection> collections, List<ItemView> items, List<PublishIssue> issues)
    {
        foreach (var item in items.Where(i => i.Checked))
        {
            var collection = collections[item.View.CollectionId];
            var errors = itemValidationService.ValidateValues(collection, item.View.Values, state.Media);
            var entity = item.Source.Ref(state.Collection(item.Source.CollectionId));

            foreach (var (key, messages) in errors)
                foreach (var message in messages)
                    issues.Add(Error("item.invalid", message, entity, field: key));
        }
    }

    /// <summary>Two live items of a collection can't share an address.</summary>
    private static void CheckItemSlugs(Dictionary<Guid, Collection> collections, List<ItemView> items, List<PublishIssue> issues)
    {
        var duplicates = items
            .GroupBy(i => (i.View.CollectionId, Slug: i.View.Slug.ToLowerInvariant()))
            .Where(g => g.Count() > 1 && g.Any(i => i.Checked));

        foreach (var group in duplicates)
        {
            var collection = collections[group.Key.CollectionId];

            foreach (var item in group.Where(i => i.InPublish))
                issues.Add(Error(
                    "item.slug_taken",
                    $"Another {collection.SingularName.ToLowerInvariant()} already uses /{collection.Slug}/{item.View.Slug}.",
                    item.Source.Ref(collection),
                    field: "slug"));
        }
    }

    /// <summary>Two live collections of a website can't share an address.</summary>
    private static void CheckCollectionSlugs(WebsiteState state, Dictionary<Guid, Collection> collections, HashSet<Guid> changed, List<PublishIssue> issues)
    {
        foreach (var id in changed)
        {
            var slug = collections[id].Slug;
            var other = collections.Values.FirstOrDefault(c => c.Id != id && string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));

            if (other is not null)
                issues.Add(Error("collection.slug_taken", $"The collection ‘{other.Name}’ already uses /{slug}.", state.Collection(id)!.Ref(), field: "slug"));
        }
    }

    /// <summary>
    /// Pages being published, and live pages that show a collection being published, against the site as it would be.
    /// Only errors count for pages that aren't in the publish: their warnings were there before.
    /// </summary>
    private void CheckPages(WebsiteState state, PublishSelection selection, SiteSnapshot site, HashSet<Guid> changed, List<PublishIssue> issues)
    {
        foreach (var page in state.Pages)
        {
            var content = state.Contents.GetValueOrDefault(page.Id);
            var inPublish = content?.HasDraft == true && selection.Pages.Contains(page.Id);
            var document = inPublish ? content!.DraftDocument : content?.IsLive == true ? content.Document : null;

            if (document is null)
                continue;

            if (!inPublish && !Shows(page, document, changed))
                continue;

            foreach (var issue in pageValidationService.Validate(document, page, site))
            {
                if (!inPublish && issue.Severity != IssueSeverity.Error)
                    continue;

                issues.Add(new PublishIssue(
                    issue.Severity,
                    issue.Code,
                    Explain(state, page, document, issue),
                    page.Ref(),
                    issue.SectionId,
                    issue.BlockId,
                    issue.Field,
                    issue.ItemIndex));
            }
        }
    }

    /// <summary>Whether the page shows one of <paramref name="collections"/>: as their detail page, or with Collection cards.</summary>
    private static bool Shows(Page page, PageDocument document, HashSet<Guid> collections) =>
        (page.CollectionId is { } own && collections.Contains(own))
        || CardCollections(document).Any(collections.Contains);

    internal static IEnumerable<Guid> CardCollections(PageDocument document) =>
        document.Sections
            .SelectMany(s => s.Blocks)
            .OfType<CollectionCardsBlock>()
            .Select(b => b.Props.CollectionId)
            .OfType<Guid>();

    /// <summary>
    /// A page issue's message, said in terms of drafts where that helps: a field or collection that isn't live yet
    /// is part of a draft, so publishing that draft too fixes it.
    /// </summary>
    private static string Explain(WebsiteState state, Page page, PageDocument document, ContentIssue issue)
    {
        if (issue.Code is "binding.page_scope" && page.CollectionId is { } own && state.Collection(own) is { IsLive: false } unpublished)
            return $"{unpublished.Name} isn't live yet. Publish it together with this page.";

        if (issue.Code != "binding.field_missing" && issue.Code != "list.collection_missing")
            return issue.Message;

        if (ReferencedField(page, document, issue) is not { } reference)
            return issue.Message;

        if (state.Collection(reference.CollectionId) is not { } collection)
            return issue.Message;

        if (!collection.IsLive)
            return $"{collection.Name} isn't live yet. Publish it together with this page.";

        if (collection.DraftFields?.FirstOrDefault(f => f.Id == reference.FieldId) is { } field && collection.Fields.All(f => f.Id != field.Id))
            return $"{page.Title} shows ‘{field.Label}’, which isn't live yet. It's part of your draft of {collection.Name}.";

        return issue.Message;
    }

    /// <summary>The collection field a binding or Collection cards issue is about.</summary>
    private static (Guid CollectionId, string FieldId)? ReferencedField(Page page, PageDocument document, ContentIssue issue)
    {
        var block = document.Sections.SelectMany(s => s.Blocks).FirstOrDefault(b => b.Id == issue.BlockId);

        if (block is null || issue.Field is null)
            return null;

        if (issue.Field.StartsWith("bindings.") && page.CollectionId is { } own && block.Bindings?.GetValueOrDefault(issue.Field["bindings.".Length..]) is { } binding)
            return (own, binding.Field);

        if (block is not CollectionCardsBlock { Props.CollectionId: { } listed } cards)
            return null;

        var fieldId = issue.Field switch
        {
            "imageField" => cards.Props.ImageField,
            "titleField" => cards.Props.TitleField,
            "textField" => cards.Props.TextField,
            _ => "",
        };

        return (listed, fieldId);
    }

    /* ---------- Needs ---------- */

    /// <summary>
    /// Drafts not in the publish that fix blocking issues: each draft is tried with the publish, and kept when it removes
    /// an error. That is followed for a few rounds, so a draft that needs another draft brings that one too.
    /// </summary>
    private List<PublishNeed> FindNeeds(WebsiteState state, PublishSelection selection, List<PublishIssue> issues)
    {
        var needs = new List<PublishNeed>();
        var current = selection;
        var blocking = ErrorKeys(issues);

        for (var round = 0; round < MaxNeedRounds && blocking.Count > 0; round++)
        {
            var added = new List<PublishNeed>();

            foreach (var candidate in Candidates(state, current))
            {
                var trial = ErrorKeys(Evaluate(state, current.With(candidate)));
                var fixedIssues = issues.Where(i => blocking.Contains(Key(i)) && !trial.Contains(Key(i))).ToList();

                if (fixedIssues.Count > 0)
                    added.Add(new PublishNeed(candidate, NeededBy(state, current, fixedIssues)));
            }

            if (added.Count == 0)
                break;

            needs.AddRange(added);
            current = added.Aggregate(current, (s, need) => s.With(need.Entity));
            issues = Evaluate(state, current);
            blocking = ErrorKeys(issues);
        }

        return needs;
    }

    /// <summary>Every draft of the website that isn't in <paramref name="selection"/>.</summary>
    private static IEnumerable<EntityRef> Candidates(WebsiteState state, PublishSelection selection) =>
        DraftDescriber.Drafts(state)
            .Select(d => d.Entity)
            .Where(e => !selection.Contains(e.Kind, e.Id));

    /// <summary>What in the publish an issue is about: the entity itself, or the collections it shows.</summary>
    private static IReadOnlyList<EntityRef> NeededBy(WebsiteState state, PublishSelection selection, List<PublishIssue> fixedIssues)
    {
        var neededBy = new List<EntityRef>();

        foreach (var issue in fixedIssues)
        {
            if (issue.Entity is not { } entity)
                continue;

            if (selection.Contains(entity.Kind, entity.Id))
                neededBy.Add(entity);
            else
                neededBy.AddRange(RelatedCollections(state, entity).Where(selection.Collections.Contains).Select(id => state.Collection(id)!.Ref()));
        }

        return neededBy.DistinctBy(e => (e.Kind, e.Id)).ToList();
    }

    private static IEnumerable<Guid> RelatedCollections(WebsiteState state, EntityRef entity)
    {
        if (entity.Kind == DraftKind.Item && entity.CollectionId is { } collectionId)
            return [collectionId];

        if (entity.Kind != DraftKind.Page || state.Page(entity.Id) is not { } page)
            return [];

        var content = state.Contents.GetValueOrDefault(page.Id);
        var documents = new[] { content?.Document, content?.DraftDocument }.OfType<PageDocument>();
        var listed = documents.SelectMany(CardCollections);

        return page.CollectionId is { } own ? listed.Append(own) : listed;
    }

    private static HashSet<string> ErrorKeys(List<PublishIssue> issues) =>
        issues.Where(i => i.Severity == IssueSeverity.Error).Select(Key).ToHashSet();

    /// <summary>What identifies an issue across checks; the message is left out because it can say more in another check.</summary>
    private static string Key(PublishIssue issue) =>
        $"{issue.Code}|{issue.Entity?.Kind}|{issue.Entity?.Id}|{issue.SectionId}|{issue.BlockId}|{issue.Field}|{issue.ItemIndex}";

    private static PublishIssue Error(string code, string message, EntityRef entity, string? field = null) =>
        new(IssueSeverity.Error, code, message, entity, Field: field);
}
