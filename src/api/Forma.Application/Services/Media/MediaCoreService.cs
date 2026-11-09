using Forma.Application.Entities.Media;
using Forma.Application.Mappers.Media;
using Forma.Infrastructure.Imaging;
using Forma.Infrastructure.Repositories.Media;
using Forma.Infrastructure.Repositories.Websites;
using Forma.Infrastructure.Storage;

namespace Forma.Application.Services.Media;

public interface IMediaCoreService
{
    Task<IReadOnlyList<MediaItem>> GetMediaAsync(Guid websiteId, CancellationToken cancellationToken = default);

    Task<MediaItem?> GetMediaItemAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the upload as an image, writes the original and a thumbnail to the assets
    /// folder and records it in the website's media library.
    /// </summary>
    Task<MediaUploadResult> UploadAsync(Guid websiteId, string fileName, long length, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Deletes the media item and its files. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class MediaCoreService(
    IMediaRepository mediaRepository,
    IWebsiteRepository websiteRepository,
    IImageProcessor imageProcessor,
    IAssetStorage assetStorage) : IMediaCoreService
{
    /// <summary>Largest accepted upload: 10 MB.</summary>
    public const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    private const int MaxFileNameLength = 255;

    public async Task<IReadOnlyList<MediaItem>> GetMediaAsync(Guid websiteId, CancellationToken cancellationToken = default)
    {
        var filter = new MediaFilter { WebsiteId = websiteId };
        var media = await mediaRepository.GetMediaAsync(filter, cancellationToken);
        return media.ToEntities();
    }

    public async Task<MediaItem?> GetMediaItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var media = await mediaRepository.GetByIdAsync(id, cancellationToken);
        return media?.ToEntity();
    }

    public async Task<MediaUploadResult> UploadAsync(
        Guid websiteId,
        string fileName,
        long length,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (length == 0)
        {
            return new MediaUploadResult.Rejected("The file is empty.");
        }

        if (length > MaxFileSizeInBytes)
        {
            return new MediaUploadResult.Rejected($"The file is larger than {MaxFileSizeInBytes / (1024 * 1024)} MB.");
        }

        if (!await websiteRepository.ExistsAsync(websiteId, cancellationToken))
        {
            return new MediaUploadResult.WebsiteNotFound();
        }

        // Buffer the upload once: it is decoded for validation and the thumbnail, then written as-is.
        using var buffer = new MemoryStream((int)length);
        await content.CopyToAsync(buffer, cancellationToken);

        buffer.Position = 0;
        var image = imageProcessor.Process(buffer);
        if (image is null)
        {
            return new MediaUploadResult.Rejected("Only JPEG, PNG, GIF and WebP images are allowed.");
        }

        var id = Guid.CreateVersion7();
        var now = DateTime.UtcNow;
        var media = new MediaItem
        {
            Id = id,
            WebsiteId = websiteId,
            FileName = SanitizeFileName(fileName, image.FileExtension),
            ContentType = image.ContentType,
            SizeInBytes = buffer.Length,
            Width = image.Width,
            Height = image.Height,
            FilePath = $"media/{websiteId}/originals/{id}{image.FileExtension}",
            ThumbnailPath = $"media/{websiteId}/thumbnails/{id}{ProcessedImage.ThumbnailFileExtension}",
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            buffer.Position = 0;
            await assetStorage.SaveAsync(media.FilePath, buffer, cancellationToken);
            using var thumbnail = new MemoryStream(image.Thumbnail, writable: false);
            await assetStorage.SaveAsync(media.ThumbnailPath, thumbnail, cancellationToken);

            await mediaRepository.AddAsync(media.ToDao(), cancellationToken);
        }
        catch
        {
            DeleteFiles(media);
            throw;
        }

        return new MediaUploadResult.Uploaded(media);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var media = await mediaRepository.GetByIdAsync(id, cancellationToken);
        if (media is null || !await mediaRepository.DeleteAsync(id, cancellationToken))
        {
            return false;
        }

        DeleteFiles(media.ToEntity());
        return true;
    }

    private void DeleteFiles(MediaItem media)
    {
        assetStorage.Delete(media.FilePath);
        assetStorage.Delete(media.ThumbnailPath);
    }

    /// <summary>Keeps only the file name part, without control characters, and falls back to a generic name.</summary>
    private static string SanitizeFileName(string fileName, string extension)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();

        if (string.IsNullOrEmpty(name))
        {
            name = $"image{extension}";
        }

        return name.Length <= MaxFileNameLength ? name : name[^MaxFileNameLength..];
    }
}
