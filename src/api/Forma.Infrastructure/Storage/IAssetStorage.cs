namespace Forma.Infrastructure.Storage;

/// <summary>Stores files in the assets folder. Paths are relative to that folder and use forward slashes.</summary>
public interface IAssetStorage
{
    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file if it exists.</summary>
    void Delete(string path);
}
