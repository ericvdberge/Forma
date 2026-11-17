using Forma.Application.Documents.Validation;
using Forma.Infrastructure.Repositories.Media;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Documents.Checks;

/// <summary>
/// The publish rules. Errors are things visitors would see as broken (an image with no picture, a button that goes
/// nowhere); warnings are worth a look but often intentional (a placeholder link, missing alt text).
/// </summary>
public class PageDocumentChecker(IMediaRepository mediaRepository, IPageRepository pageRepository) : IPageDocumentChecker
{
    public async Task<IReadOnlyList<ContentIssue>> CheckAsync(PageDocument document, CheckContext context, CancellationToken cancellationToken = default)
    {
        // Everything the rules look up, loaded once: the website's media and its page slugs.
        var media = (await mediaRepository.GetMediaItemsAsync(new MediaFilter { WebsiteId = context.WebsiteId }, cancellationToken))
            .Select(m => m.Id)
            .ToHashSet();
        var slugs = (await pageRepository.GetPagesAsync(new PageFilter { WebsiteId = context.WebsiteId }, cancellationToken))
            .Select(p => p.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var issues = new List<ContentIssue>();
        new Walker(issues, media, slugs).Visit(document);

        // Errors first, then in page order (List.Sort isn't stable, OrderBy is).
        return issues.OrderBy(i => i.Severity).ToList();
    }

    private sealed class Walker(List<ContentIssue> issues, HashSet<Guid> media, HashSet<string> slugs)
    {
        private Section _section = null!;
        private Block? _block;
        private int _lastHeadingLevel;
        private bool _seenH1;

        public void Visit(PageDocument document)
        {
            if (document.Sections.Count == 0)
            {
                issues.Add(new(IssueSeverity.Warning, "page.empty", "The page has no content yet."));
            }

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
            {
                Warn("section.empty", "This section is empty, so it shows as a blank band.");
            }

            var background = section.Settings.Background;
            if (background.Kind == BackgroundKind.Image)
            {
                if (background.Image.Length == 0)
                {
                    Warn("background.image_missing", "The background is set to an image, but no image is chosen.", "background.image");
                }
                else
                {
                    CheckMedia(background.Image, "background.image");
                }
            }
        }

        private void VisitBlock(Block block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    CheckHeading(heading.Props.Level);
                    break;
                case ButtonBlock button:
                    CheckLink(button.Props.Href, "href", $"The button ‘{button.Props.Label}’");
                    break;
                case ImageBlock image:
                    CheckImage(image.Props.Src, "src");
                    if (image.Props.Src.Length > 0 && image.Props.Alt.Trim().Length == 0)
                    {
                        Warn("image.alt_missing", "This image has no description for people who can't see it.", "alt");
                    }
                    break;
                case GalleryBlock gallery:
                    if (gallery.Props.Images.Count == 0)
                    {
                        Error("gallery.empty", "This gallery has no images.", "images");
                    }
                    for (var i = 0; i < gallery.Props.Images.Count; i++)
                    {
                        CheckImage(gallery.Props.Images[i].Src, "images", i);
                    }
                    break;
                case CarouselBlock carousel:
                    if (carousel.Props.Slides.Count == 0)
                    {
                        Error("carousel.empty", "This carousel has no slides.", "slides");
                    }
                    for (var i = 0; i < carousel.Props.Slides.Count; i++)
                    {
                        CheckImage(carousel.Props.Slides[i].Src, "slides", i);
                    }
                    break;
                case VideoBlock video when video.Props.Url.Length == 0:
                    Error("video.missing", "This video has no link yet.", "url");
                    break;
                case FormBlock form when form.Props.Fields.Count == 0:
                    Error("form.no_fields", "This form has no fields to fill in.", "fields");
                    break;
                case NavBlock nav:
                    for (var i = 0; i < nav.Props.Links.Count; i++)
                    {
                        CheckLink(nav.Props.Links[i].Href, "links", $"The menu link ‘{nav.Props.Links[i].Label}’", i);
                    }
                    // The call-to-action only shows when it has a label.
                    if (nav.Props.CtaLabel.Trim().Length > 0)
                    {
                        CheckLink(nav.Props.CtaHref, "ctaHref", $"The menu button ‘{nav.Props.CtaLabel}’");
                    }
                    break;
            }
        }

        private void CheckHeading(int level)
        {
            if (level == 1 && _seenH1)
            {
                Warn("heading.multiple_h1", "The page already has a main (H1) heading; search engines expect one.", "level");
            }
            else if (_lastHeadingLevel > 0 && level > _lastHeadingLevel + 1)
            {
                Warn("heading.skipped_level", $"This H{level} follows an H{_lastHeadingLevel}; screen readers expect no levels to be skipped.", "level");
            }

            _seenH1 |= level == 1;
            _lastHeadingLevel = level;
        }

        private void CheckImage(string src, string field, int? item = null)
        {
            if (src.Length == 0)
            {
                Error("image.missing", "This image has no picture yet.", field, item);
            }
            else
            {
                CheckMedia(src, field, item);
            }
        }

        private void CheckMedia(string src, string field, int? item = null)
        {
            if (DocumentRules.MediaId(src) is { } id && !media.Contains(id))
            {
                Error("media.not_found", "This image was deleted from the media library.", field, item);
            }
        }

        private void CheckLink(string href, string field, string what, int? item = null)
        {
            if (href.Trim().Length == 0)
            {
                Error("link.empty", $"{what} doesn't link anywhere.", field, item);
            }
            else if (href == "#")
            {
                Warn("link.placeholder", $"{what} still links to # (nowhere).", field, item);
            }
            else if (href.StartsWith('/') && !IsPage(href))
            {
                Warn("link.page_missing", $"{what} links to {href}, which isn't a page of this website.", field, item);
            }
        }

        /// <summary>Whether a site-relative link points at one of the website's pages (<c>/</c> is the home page).</summary>
        private bool IsPage(string href)
        {
            var path = href.Split('#', '?')[0].Trim('/');
            return path.Length == 0 || slugs.Contains(path);
        }

        private void Error(string code, string message, string? field = null, int? item = null) =>
            issues.Add(new(IssueSeverity.Error, code, message, _section.Id, _block?.Id, field, item));

        private void Warn(string code, string message, string? field = null, int? item = null) =>
            issues.Add(new(IssueSeverity.Warning, code, message, _section.Id, _block?.Id, field, item));
    }
}
