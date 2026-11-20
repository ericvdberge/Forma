using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Forma.Application.Documents;

/// <summary>Turns documents into the stored JSON and back.</summary>
public static class PageDocumentSerializer
{
    /// <summary>The canonical JSON of <paramref name="document"/> and its SHA-256 (lowercase hex).</summary>
    /// <remarks>Equal documents always give equal JSON, so the hash tells whether a draft differs from the published version.</remarks>
    public static (string Json, string Hash) Serialize(PageDocument document)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(document, PageDocumentJson.Options);
        return (Encoding.UTF8.GetString(bytes), Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    /// <summary>Reads a stored document, upgrading older schema versions to <see cref="PageDocument.CurrentVersion"/>.</summary>
    /// <exception cref="InvalidOperationException">The stored version is newer than this build understands.</exception>
    public static PageDocument Deserialize(string json, int schemaVersion)
    {
        if (schemaVersion > PageDocument.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"The stored document has schema version {schemaVersion}; this build reads up to {PageDocument.CurrentVersion}.");
        }

        // Upgraders (v1 → v2 …) go here, each rewriting the JSON before it is read into the current model.
        return JsonSerializer.Deserialize<PageDocument>(json, PageDocumentJson.Options)
            ?? throw new InvalidOperationException("The stored document is empty.");
    }
}
