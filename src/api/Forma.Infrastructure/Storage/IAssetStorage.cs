namespace Forma.Infrastructure.Storage;

/// <summary>Stores uploaded files by key, e.g. <c>{websiteId}/originals/{id}.jpg</c>.</summary>
public interface IAssetStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Opens the file for reading, or returns <c>null</c> when it does not exist. Dispose the result.</summary>
    Task<StoredAsset?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file if it exists.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public sealed record StoredAsset(Stream Content, string ContentType, long Length) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
