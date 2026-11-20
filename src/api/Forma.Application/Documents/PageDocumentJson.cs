using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forma.Application.Documents;

/// <summary>
/// JSON settings for page documents. The API's HTTP options and the storage serialiser both use them,
/// so a document is read the same way from a request and from the database.
/// </summary>
public static class PageDocumentJson
{
    /// <summary>Options for storing documents (canonical form: the stored content hash depends on it).</summary>
    public static JsonSerializerOptions Options { get; } = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    /// <summary>Applies the document rules to <paramref name="options"/>.</summary>
    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        // camelCase enums; members with [JsonStringEnumMemberName] ("fade-up", "16/9") keep their given name.
        if (!options.Converters.OfType<JsonStringEnumConverter>().Any())
        {
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        // Unknown and misspelled properties are errors, not silently dropped data.
        options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;

        // A missing constructor parameter (the props records) or a null in a non-nullable property is an error,
        // not a silent default; `required` properties are already enforced.
        options.RespectRequiredConstructorParameters = true;
        options.RespectNullableAnnotations = true;

        // Numbers are JSON numbers. The web default also accepts "12", which makes the generated TS types `number | string`.
        options.NumberHandling = JsonNumberHandling.Strict;

        // The studio writes a block's "id" before its "type" discriminator.
        options.AllowOutOfOrderMetadataProperties = true;

        return options;
    }
}
