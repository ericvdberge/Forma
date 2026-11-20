using System.Globalization;
using System.Text.Json;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Infrastructure.Repositories.CollectionItems;

namespace Forma.Application.Services;

/// <summary>
/// The rules an item's values must pass, built from its collection's fields (<c>docs/design/collections.md</c> § 5):
/// every value matches its field's type, required fields are set, unknown fields are rejected, and text, links,
/// images and rich text follow the same rules as page documents. Errors are keyed by property
/// (<c>slug</c>, <c>values.fld_…</c>), ready for a <c>400</c> validation problem.
/// </summary>
public interface ICollectionItemValidationService
{
    /// <summary>
    /// The errors of an item of <paramref name="collection"/> with <paramref name="slug"/> and <paramref name="values"/>;
    /// <paramref name="itemId"/> is the item being changed, or <c>null</c> for a new one. Empty when there are none.
    /// </summary>
    Task<IDictionary<string, string[]>> ValidateAsync(
        Collection collection,
        string slug,
        IReadOnlyDictionary<string, JsonElement> values,
        Guid? itemId,
        CancellationToken cancellationToken = default);
}

public class CollectionItemValidationService(
    ICollectionItemCoreService itemCoreService,
    IMediaCoreService mediaCoreService,
    IRichTextSanitizer sanitizer) : ICollectionItemValidationService
{
    public async Task<IDictionary<string, string[]>> ValidateAsync(
        Collection collection,
        string slug,
        IReadOnlyDictionary<string, JsonElement> values,
        Guid? itemId,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        await CheckSlugAsync(collection, slug, itemId, errors, cancellationToken);

        CheckUnknownFields(collection, values, errors);

        var media = await mediaCoreService.GetMediaItemsAsync(collection.WebsiteId, cancellationToken);
        var mediaIds = media.Select(m => m.Id).ToHashSet();

        foreach (var field in collection.Fields)
        {
            var key = $"values.{field.Id}";
            var error = values.TryGetValue(field.Id, out var value) && !IsEmpty(value)
                ? CheckValue(field, value, mediaIds)
                : field.Required ? $"‘{field.Label}’ is required." : null;

            if (error is not null)
                errors[key] = [error];
        }

        return errors;
    }

    private async Task CheckSlugAsync(Collection collection, string slug, Guid? itemId, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        var existing = await itemCoreService.GetCollectionItemAsync(new CollectionItemFilter { CollectionId = collection.Id, Slug = slug }, cancellationToken);

        if (existing is not null && existing.Id != itemId)
            errors["slug"] = [$"Another {collection.SingularName.ToLowerInvariant()} already uses /{collection.Slug}/{slug}."];
    }

    private static void CheckUnknownFields(Collection collection, IReadOnlyDictionary<string, JsonElement> values, Dictionary<string, string[]> errors)
    {
        var known = collection.Fields.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var id in values.Keys.Where(id => !known.Contains(id)))
            errors[$"values.{id}"] = [$"{collection.Name} has no field ‘{id}’."];
    }

    /// <summary>Null and empty strings count as "not filled in".</summary>
    private static bool IsEmpty(JsonElement value) =>
        value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
        || (value.ValueKind == JsonValueKind.String && value.GetString()!.Trim().Length == 0);

    /// <summary>The problem with a filled-in value, or <c>null</c> when it is fine.</summary>
    private string? CheckValue(CollectionField field, JsonElement value, HashSet<Guid> mediaIds) => field.Type switch
    {
        CollectionFieldType.Text => CheckText(value),
        CollectionFieldType.RichText => CheckRichText(value),
        CollectionFieldType.Image => CheckImage(value, mediaIds),
        CollectionFieldType.Url => CheckUrl(value),
        CollectionFieldType.Number => value.ValueKind == JsonValueKind.Number ? null : "Enter a number.",
        CollectionFieldType.Date => CheckDate(value),
        CollectionFieldType.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : "Choose yes or no.",
        _ => "This field type isn't supported.",
    };

    private static string? CheckText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            return "Enter text.";

        if (value.GetString()!.Length > DocumentRules.MaxTextLength)
            return $"Use at most {DocumentRules.MaxTextLength} characters.";

        return null;
    }

    private string? CheckRichText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            return "Enter text.";

        var html = value.GetString()!;

        if (html.Length > DocumentRules.MaxHtmlLength)
            return "The text is too long.";

        if (!sanitizer.IsClean(html))
            return "The text contains markup that isn't allowed.";

        return null;
    }

    private static string? CheckImage(JsonElement value, HashSet<Guid> mediaIds)
    {
        if (value.ValueKind != JsonValueKind.String || !DocumentRules.IsImageSource(value.GetString()!))
            return "Images must come from the media library.";

        if (DocumentRules.MediaId(value.GetString()!) is { } id && !mediaIds.Contains(id))
            return "This image isn't in the website's media library.";

        return null;
    }

    private static string? CheckUrl(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            return "Enter a link.";

        var url = value.GetString()!;

        if (url.Length > DocumentRules.MaxUrlLength || !DocumentRules.IsLink(url))
            return "Links must start with https://, http://, mailto:, tel:, / or #.";

        return null;
    }

    private static string? CheckDate(JsonElement value)
    {
        var isDate = value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

        return isDate ? null : "Enter a date as yyyy-mm-dd.";
    }
}
