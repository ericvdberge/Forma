namespace Forma.Infrastructure.Storage;

/// <summary>Keeps assets on the local file system, under <see cref="AssetStorageOptions.RootPath"/>.</summary>
public class LocalAssetStorage(AssetStorageOptions options) : IAssetStorage
{
    private readonly string _rootPath = Path.GetFullPath(options.RootPath);

    public async Task SaveAsync(string path, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public void Delete(string path)
    {
        var fullPath = ResolvePath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string ResolvePath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, path));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Path '{path}' is outside the assets folder.", nameof(path));
        }

        return fullPath;
    }
}
