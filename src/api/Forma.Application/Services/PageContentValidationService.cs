using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Infrastructure.Repositories.Collections;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Services;

/// <summary>
/// The rules a page's content must pass before it is published, on top of the document's structural validation.
/// Errors are things visitors would see as broken (an image with no picture, a button that goes nowhere);
/// warnings are worth a look but often intentional (a placeholder link, missing alt text).
/// Collection bindings and lists are checked against the website's collections (<c>docs/design/collections.md</c> § 5).
/// </summary>
public interface IPageContentValidationService
{
    /// <summary>The issues of <paramref name="document"/> on <paramref name="page"/>: errors first, then warnings.</summary>
    Task<IReadOnlyList<ContentIssue>> ValidateAsync(PageDocument document, Page page, CancellationToken cancellationToken = default);
}

public class PageContentValidationService(
    IMediaCoreService mediaCoreService,
    IPageCoreService pageCoreService,
    ICollectionCoreService collectionCoreService) : IPageContentValidationService
{
    public async Task<IReadOnlyList<ContentIssue>> ValidateAsync(PageDocument document, Page page, CancellationToken cancellationToken = default)
    {
        // Everything the rules look up, loaded once: the website's media, page slugs and collections.
        var media = await mediaCoreService.GetMediaItemsAsync(page.WebsiteId, cancellationToken);
        var pages = await pageCoreService.GetPagesAsync(new PageFilter { WebsiteId = page.WebsiteId }, cancellationToken);
        var collections = await collectionCoreService.GetCollectionsAsync(new CollectionFilter { WebsiteId = page.WebsiteId }, cancellationToken);

        var lookups = new Lookups(
            Media: media.Select(m => m.Id).ToHashSet(),
            Slugs: pages.Where(p => p.Kind == PageKind.Static).Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase),
            Collections: collections.ToDictionary(c => c.Id),
            PageCollection: collections.FirstOrDefault(c => page.Kind == PageKind.CollectionTemplate && c.Id == page.CollectionId));

        var issues = new List<ContentIssue>();
        new Walker(issues, lookups).Visit(document);

        // Errors first, then in page order (List.Sort isn't stable, OrderBy is).
        return issues.OrderBy(i => i.Severity).ToList();
    }

    /// <param name="Media">Ids of the website's media items.</param>
    /// <param name="Slugs">Slugs of the website's own pages.</param>
    /// <param name="Collections">The website's collections by id.</param>
    /// <param name="PageCollection">The collection whose items the page shows, when it is a collection's detail page.</param>
    private sealed record Lookups(
        HashSet<Guid> Media,
        HashSet<string> Slugs,
        Dictionary<Guid, Collection> Collections,
        Collection? PageCollection);

    private sealed class Walker(List<ContentIssue> issues, Lookups lookups)
    {
        private Section _section = null!;
        private Block? _block;
        private int _lastHeadingLevel;
        private bool _seenH1;

        public void Visit(PageDocument document)
        {
            if (document.Sections.Count == 0)
                issues.Add(new(IssueSeverity.Warning, "page.empty", "The page has no content yet."));

            foreach (var section in document.Sections)
            {
                _section = section;
                _block = null;
                VisitSection(section);
                foreach (var block in section.Blocks)
                {
                    _block = block;
                    VisitBlock(block);
                }
            }
        }

        private void VisitSection(Section section)
        {
            if (section.Blocks.Count == 0)
                Warn("section.empty", "This section is empty, so it shows as a blank band.");

            var background = section.Settings.Background;
            if (background.Kind == BackgroundKind.Image)
            {
                if (background.Image.Length == 0)
                    Warn("background.image_missing", "The background is set to an image, but no image is chosen.", "background.image");
                else
                    CheckMedia(background.Image, "background.image");
            }
        }

        private void VisitBlock(Block block)
        {
            CheckBindings(block);

            switch (block)
            {
                case HeadingBlock heading:
                    CheckHeading(heading.Props.Level);
                    break;
                case ButtonBlock button when !IsBound(block, "href"):
                    CheckLink(button.Props.Href, "href", $"The button ‘{button.Props.Label}’");
                    break;
                case ImageBlock image when IsBound(block, "src"):
                    CheckMedia(image.Props.Src, "src");
                    break;
                case ImageBlock image:
                    CheckImage(image.Props.Src, "src");
                    if (image.Props.Src.Length > 0 && image.Props.Alt.Trim().Length == 0 && !IsBound(block, "alt"))
                        Warn("image.alt_missing", "This image has no description for people who can't see it.", "alt");
                    break;
                case GalleryBlock gallery:
                    if (gallery.Props.Images.Count == 0)
                        Error("gallery.empty", "This gallery has no images.", "images");

                    for (var i = 0; i < gallery.Props.Images.Count; i++)
                        CheckImage(gallery.Props.Images[i].Src, "images", i);

                    break;
                case CarouselBlock carousel:
                    if (carousel.Props.Slides.Count == 0)
                        Error("carousel.empty", "This carousel has no slides.", "slides");

                    for (var i = 0; i < carousel.Props.Slides.Count; i++)
                        CheckImage(carousel.Props.Slides[i].Src, "slides", i);

                    break;
                case VideoBlock video when video.Props.Url.Length == 0 && !IsBound(block, "url"):
                    Error("video.missing", "This video has no link yet.", "url");
                    break;
                case FormBlock form when form.Props.Fields.Count == 0:
                    Error("form.no_fields", "This form has no fields to fill in.", "fields");
                    break;
                case NavBlock nav:
                    for (var i = 0; i < nav.Props.Links.Count; i++)
                        CheckLink(nav.Props.Links[i].Href, "links", $"The menu link ‘{nav.Props.Links[i].Label}’", i);

                    // The call-to-action only shows when it has a label.
                    if (nav.Props.CtaLabel.Trim().Length > 0)
                        CheckLink(nav.Props.CtaHref, "ctaHref", $"The menu button ‘{nav.Props.CtaLabel}’");

                    break;
                case CollectionCardsBlock cards:
                    CheckCollectionCards(cards.Props);
                    break;
            }
        }

        private static bool IsBound(Block block, string prop) => block.Bindings?.ContainsKey(prop) == true;

        /// <summary>Bound props need a detail page, and a field of its collection that can feed them.</summary>
        private void CheckBindings(Block block)
        {
            foreach (var (prop, binding) in block.Bindings ?? new Dictionary<string, Binding>())
            {
                var field = $"bindings.{prop}";

                if (lookups.PageCollection is not { } collection)
                {
                    Error("binding.page_scope", "This is connected to a collection field, but the page isn't a collection's detail page.", field);
                    continue;
                }

                var definition = collection.Fields.FirstOrDefault(f => f.Id == binding.Field);

                if (definition is null)
                    Error("binding.field_missing", $"This is connected to a field that was removed from {collection.Name}.", field);
                else if (!BindableProps.Accepts(block, prop, definition.Type))
                    Error("binding.type_mismatch", $"The field ‘{definition.Label}’ can't be shown here.", field);
                else if (!definition.Required)
                    Warn("binding.optional_field", $"‘{definition.Label}’ isn't required, so some items will show the placeholder.", field);
            }
        }

        private void CheckCollectionCards(CollectionCardsProps props)
        {
            if (props.CollectionId is not { } id)
            {
                Error("list.collection_missing", "Choose which collection these cards show.", "collectionId");
                return;
            }

            if (!lookups.Collections.TryGetValue(id, out var collection))
            {
                Error("list.collection_missing", "The collection these cards show was deleted.", "collectionId");
                return;
            }

            CheckCardField(collection, props.ImageField, "imageField", [CollectionFieldType.Image]);
            CheckCardField(collection, props.TitleField, "titleField", [CollectionFieldType.Text, CollectionFieldType.RichText, CollectionFieldType.Number, CollectionFieldType.Date]);
            CheckCardField(collection, props.TextField, "textField", [CollectionFieldType.Text, CollectionFieldType.RichText, CollectionFieldType.Number, CollectionFieldType.Date]);
        }

        private void CheckCardField(Collection collection, string fieldId, string prop, CollectionFieldType[] types)
        {
            if (fieldId.Length == 0)
                return;

            var definition = collection.Fields.FirstOrDefault(f => f.Id == fieldId);

            if (definition is null)
                Error("binding.field_missing", $"The cards use a field that was removed from {collection.Name}.", prop);
            else if (!types.Contains(definition.Type))
                Error("binding.type_mismatch", $"The field ‘{definition.Label}’ can't be shown here.", prop);
        }

        private void CheckHeading(int level)
        {
            if (level == 1 && _seenH1)
                Warn("heading.multiple_h1", "The page already has a main (H1) heading; search engines expect one.", "level");
            else if (_lastHeadingLevel > 0 && level > _lastHeadingLevel + 1)
                Warn("heading.skipped_level", $"This H{level} follows an H{_lastHeadingLevel}; screen readers expect no levels to be skipped.", "level");

            _seenH1 |= level == 1;
            _lastHeadingLevel = level;
        }

        private void CheckImage(string src, string field, int? item = null)
        {
            if (src.Length == 0)
                Error("image.missing", "This image has no picture yet.", field, item);
            else
                CheckMedia(src, field, item);
        }

        private void CheckMedia(string src, string field, int? item = null)
        {
            if (DocumentRules.MediaId(src) is { } id && !lookups.Media.Contains(id))
                Error("media.not_found", "This image was deleted from the media library.", field, item);
        }

        private void CheckLink(string href, string field, string what, int? item = null)
        {
            if (href.Trim().Length == 0)
                Error("link.empty", $"{what} doesn't link anywhere.", field, item);
            else if (href == "#")
                Warn("link.placeholder", $"{what} still links to # (nowhere).", field, item);
            else if (href.StartsWith('/') && !IsPage(href))
                Warn("link.page_missing", $"{what} links to {href}, which isn't a page of this website.", field, item);
        }

        /// <summary>
        /// Whether a site-relative link points at one of the website's pages (<c>/</c> is the home page) or at an item
        /// of one of its collections (<c>/blog/opening-our-studio</c>).
        /// </summary>
        private bool IsPage(string href)
        {
            var path = href.Split('#', '?')[0].Trim('/');
            if (path.Length == 0 || lookups.Slugs.Contains(path))
                return true;

            var segments = path.Split('/');

            return segments.Length == 2 && lookups.Collections.Values.Any(c => string.Equals(c.Slug, segments[0], StringComparison.OrdinalIgnoreCase));
        }

        private void Error(string code, string message, string? field = null, int? item = null) =>
            issues.Add(new(IssueSeverity.Error, code, message, _section.Id, _block?.Id, field, item));

        private void Warn(string code, string message, string? field = null, int? item = null) =>
            issues.Add(new(IssueSeverity.Warning, code, message, _section.Id, _block?.Id, field, item));
    }
}
