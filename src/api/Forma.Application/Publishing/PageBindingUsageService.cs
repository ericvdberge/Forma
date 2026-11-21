using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.Collections;
using Forma.Infrastructure.Repositories.PageBindingUsages;
using Forma.Infrastructure.Repositories.PageContents;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Publishing;

/// <summary>
/// Keeps <see cref="PageBindingUsage"/> in step with live content: which collection fields each page shows, for
/// "Used by…" in the field editor.
/// </summary>
public interface IPageBindingUsageService
{
    /// <summary>Replaces the usages of <paramref name="page"/> with those of its live <paramref name="document"/>.</summary>
    Task RebuildAsync(Page page, PageDocument document, CancellationToken cancellationToken = default);

    /// <summary>Builds the usages of every live page when there are none yet, e.g. after the migration that added them.</summary>
    Task RebuildAllIfEmptyAsync(CancellationToken cancellationToken = default);
}

public class PageBindingUsageService(
    IPageBindingUsageCoreService usageCoreService,
    IPageCoreService pageCoreService,
    IPageContentCoreService pageContentCoreService,
    ICollectionCoreService collectionCoreService) : IPageBindingUsageService
{
    public async Task RebuildAsync(Page page, PageDocument document, CancellationToken cancellationToken = default)
    {
        var collections = await CollectionIdsAsync(new CollectionFilter { WebsiteId = page.WebsiteId }, cancellationToken);

        await usageCoreService.DeleteAsync(page.Id, cancellationToken);

        await usageCoreService.CreateAsync(UsagesOf(page, document, collections), cancellationToken);
    }

    public async Task RebuildAllIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        var existing = await usageCoreService.GetPageBindingUsagesAsync(new PageBindingUsageFilter(), cancellationToken);
        if (existing.Count > 0)
            return;

        var pages = await pageCoreService.GetPagesAsync(new PageFilter(), cancellationToken);
        var contents = await pageContentCoreService.GetPageContentsAsync(new PageContentFilter { IsLive = true }, cancellationToken);
        var collections = await CollectionIdsAsync(new CollectionFilter(), cancellationToken);

        foreach (var content in contents.Where(c => c.Document is not null))
        {
            var page = pages.FirstOrDefault(p => p.Id == content.PageId);

            if (page is not null)
                await usageCoreService.CreateAsync(UsagesOf(page, content.Document!, collections), cancellationToken);
        }
    }

    private async Task<HashSet<Guid>> CollectionIdsAsync(CollectionFilter filter, CancellationToken cancellationToken)
    {
        var collections = await collectionCoreService.GetCollectionsAsync(filter, cancellationToken);
        return collections.Select(c => c.Id).ToHashSet();
    }

    /// <summary>Connected blocks on a detail page, and the fields Collection cards show, of the <paramref name="collections"/> that exist.</summary>
    public static IReadOnlyList<PageBindingUsage> UsagesOf(Page page, PageDocument document, IReadOnlySet<Guid> collections)
    {
        var blocks = document.Sections.SelectMany(s => s.Blocks).ToList();
        var usages = new List<PageBindingUsage>();

        if (page.CollectionId is { } own)
            foreach (var binding in blocks.SelectMany(b => b.Bindings?.Values ?? []))
                usages.Add(new(page.Id, own, binding.Field, PageBindingUsageKind.Binding));

        foreach (var cards in blocks.OfType<CollectionCardsBlock>())
        {
            if (cards.Props.CollectionId is not { } listed)
                continue;

            foreach (var field in new[] { cards.Props.ImageField, cards.Props.TitleField, cards.Props.TextField }.Where(f => f.Length > 0))
                usages.Add(new(page.Id, listed, field, PageBindingUsageKind.Cards));
        }

        return usages.Where(u => collections.Contains(u.CollectionId)).Distinct().ToList();
    }
}
