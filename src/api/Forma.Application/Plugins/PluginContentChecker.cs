using System.Text.Json;
using System.Text.RegularExpressions;
using Forma.Application.Documents;
using Forma.Application.Documents.Checks;
using Forma.Infrastructure.Plugins;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Plugins;

/// <summary>
/// The built-in publish rules plus the issues of every plugin subscribed to the <see cref="PluginEvents.PageCheck"/> hook.
/// </summary>
/// <remarks>
/// A plugin's issues are trusted only as far as they can be checked: codes are prefixed with the plugin id, messages
/// are length-limited, and pointers to sections or blocks that aren't in the page are dropped. A plugin that fails
/// adds a warning instead of blocking the page: a broken plugin shouldn't stop people from publishing.
/// </remarks>
public sealed partial class PluginContentChecker(
    PageDocumentChecker builtIn,
    IPluginCatalog catalog,
    IPluginRunner runner,
    IPageRepository pageRepository,
    TimeProvider timeProvider) : IPageDocumentChecker
{
    /// <summary>Issues kept per plugin per check.</summary>
    public const int MaxIssuesPerPlugin = 50;

    private const int MaxMessageLength = 300;

    private static readonly JsonSerializerOptions OutputJson = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    public async Task<IReadOnlyList<ContentIssue>> CheckAsync(PageDocument document, CheckContext context, CancellationToken cancellationToken = default)
    {
        var issues = (await builtIn.CheckAsync(document, context, cancellationToken)).ToList();

        if (!catalog.Plugins.Any(p => p.IsLoaded && p.Manifest!.Events.Contains(PluginEvents.PageCheck))
            || await pageRepository.GetByIdAsync(context.PageId, cancellationToken) is not { } page)
        {
            return issues;
        }

        var envelope = new PluginEventEnvelope(
            PluginEvents.PageCheck,
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            new PageCheckData(new PluginPageInfo(page.Id, page.WebsiteId, page.Title, page.Slug), document));

        // Which section each block is in, to fill in or correct the plugin's pointers.
        var sectionOfBlock = document.Sections
            .SelectMany(s => s.Blocks.Select(b => (Block: b.Id, Section: s.Id)))
            .ToDictionary(x => x.Block, x => x.Section);
        var sectionIds = document.Sections.Select(s => s.Id).ToHashSet();

        foreach (var (plugin, run) in runner.Run(envelope))
        {
            issues.AddRange(ReadIssues(plugin.Manifest!, run.Invocation, sectionIds, sectionOfBlock));
        }

        // Errors first; within a severity built-in issues come before plugin issues (OrderBy is stable).
        return issues.OrderBy(i => i.Severity).ToList();
    }

    private static IEnumerable<ContentIssue> ReadIssues(
        PluginManifest plugin,
        PluginInvocation invocation,
        HashSet<string> sectionIds,
        Dictionary<string, string> sectionOfBlock)
    {
        if (!invocation.Succeeded)
        {
            return [Failed(plugin)];
        }

        if (invocation.Output is null)
        {
            return [];
        }

        PageCheckOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<PageCheckOutput>(invocation.Output, OutputJson);
        }
        catch (JsonException)
        {
            return [Failed(plugin)];
        }

        if (output?.Issues is null)
        {
            return [Failed(plugin)];
        }

        var issues = new List<ContentIssue>();
        foreach (var issue in output.Issues.Take(MaxIssuesPerPlugin))
        {
            if (ToIssue(plugin, issue, sectionIds, sectionOfBlock) is { } valid)
            {
                issues.Add(valid);
            }
        }

        return issues;
    }

    private static ContentIssue? ToIssue(PluginManifest plugin, PageCheckOutputIssue issue, HashSet<string> sectionIds, Dictionary<string, string> sectionOfBlock)
    {
        IssueSeverity? severity = issue.Severity switch
        {
            "error" => IssueSeverity.Error,
            "warning" => IssueSeverity.Warning,
            _ => null,
        };
        if (severity is null || !CodePattern().IsMatch(issue.Code) || string.IsNullOrWhiteSpace(issue.Message))
        {
            return null;
        }

        var message = issue.Message.Trim();
        if (message.Length > MaxMessageLength)
        {
            message = message[..MaxMessageLength] + "…";
        }

        // A block pointer wins: it also tells which section. Unknown ids are dropped, not passed on to the studio.
        string? sectionId = null, blockId = null;
        if (issue.BlockId is not null && sectionOfBlock.TryGetValue(issue.BlockId, out var blockSection))
        {
            blockId = issue.BlockId;
            sectionId = blockSection;
        }
        else if (issue.SectionId is not null && sectionIds.Contains(issue.SectionId))
        {
            sectionId = issue.SectionId;
        }

        var field = issue.Field is { Length: > 0 and <= 100 } && (blockId is not null || sectionId is not null) ? issue.Field : null;
        var itemIndex = field is not null && issue.ItemIndex is >= 0 and < 1000 ? issue.ItemIndex : null;

        return new ContentIssue(severity.Value, $"{plugin.Id}:{issue.Code}", message, sectionId, blockId, field, itemIndex);
    }

    private static ContentIssue Failed(PluginManifest plugin) =>
        new(IssueSeverity.Warning, "plugin.failed", $"The plugin “{plugin.Name}” couldn't check this page. Its run history says why.");

    /// <summary>A plugin's own code, e.g. <c>link.insecure</c>; it is stored as <c>{pluginId}:{code}</c>.</summary>
    [GeneratedRegex("^[a-z][a-zA-Z0-9._-]{0,59}$")]
    private static partial Regex CodePattern();
}
