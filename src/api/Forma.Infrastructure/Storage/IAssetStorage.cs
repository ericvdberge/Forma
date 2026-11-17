namespace Forma.Infrastructure.Storage;

/// <summary>Stores uploaded files by key, e.g. <c>{websiteId}/originals/{id}.jpg</c>.</summary>
public interface IAssetStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens the file for reading, or returns <c>null</c> when it does not exist. Dispose the stream.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file if it exists.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
