using Forma.Application.Entities.Media;
using Forma.Application.Mappers.Media;
using Forma.Infrastructure.Imaging;
using Forma.Infrastructure.Repositories.Media;
using Forma.Infrastructure.Repositories.Websites;
using Forma.Infrastructure.Storage;

namespace Forma.Application.Services.Media;

public interface IMediaCoreService
{
    Task<IReadOnlyList<MediaItem>> GetMediaItemsAsync(Guid websiteId, CancellationToken cancellationToken = default);

    Task<MediaItem?> GetMediaItemAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Opens the original image, or returns <c>null</c> when the media item does not exist.</summary>
    Task<MediaFile?> GetImageAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Opens the WebP thumbnail, or returns <c>null</c> when the media item does not exist.</summary>
    Task<MediaFile?> GetThumbnailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks that the upload really is a supported image, stores the original and a thumbnail
    /// and records it in the website's media library. Size and file name are validated by the caller.
    /// </summary>
    Task<MediaUploadResult> UploadAsync(Guid websiteId, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Deletes the media item and its files. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class MediaCoreService(
    IMediaRepository mediaRepository,
    IWebsiteRepository websiteRepository,
    IImageProcessor imageProcessor,
    IAssetStorage assetStorage) : IMediaCoreService
{
    public async Task<IReadOnlyList<MediaItem>> GetMediaItemsAsync(Guid websiteId, CancellationToken cancellationToken = default)
    {
        var filter = new MediaFilter { WebsiteId = websiteId };
        var media = await mediaRepository.GetMediaItemsAsync(filter, cancellationToken);
        return media.ToEntities();
    }

    public async Task<MediaItem?> GetMediaItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var media = await mediaRepository.GetByIdAsync(id, cancellationToken);
        return media?.ToEntity();
    }

    public async Task<MediaFile?> GetImageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var media = await mediaRepository.GetByIdAsync(id, cancellationToken);
        return media is null ? null : await OpenAsync(media.ImagePath, cancellationToken);
    }

    public async Task<MediaFile?> GetThumbnailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var media = await mediaRepository.GetByIdAsync(id, cancellationToken);
        return media is null ? null : await OpenAsync(media.ThumbnailPath, cancellationToken);
    }

    public async Task<MediaUploadResult> UploadAsync(
        Guid websiteId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!await websiteRepository.ExistsAsync(websiteId, cancellationToken))
        {
            return new MediaUploadResult.WebsiteNotFound();
        }

        // Buffer the upload once: it is decoded for validation and the thumbnail, then stored as-is.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var image = imageProcessor.Process(bytes);
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
            FileName = Path.GetFileName(fileName),
            ContentType = image.ContentType,
            SizeInBytes = bytes.Length,
            Width = image.Width,
            Height = image.Height,
            ImagePath = $"{websiteId}/originals/{id}{image.FileExtension}",
            ThumbnailPath = $"{websiteId}/thumbnails/{id}{ProcessedImage.ThumbnailFileExtension}",
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            using var original = new MemoryStream(bytes, writable: false);
            await assetStorage.SaveAsync(media.ImagePath, original, media.ContentType, cancellationToken);
            using var thumbnail = new MemoryStream(image.Thumbnail, writable: false);
            await assetStorage.SaveAsync(media.ThumbnailPath, thumbnail, ProcessedImage.ThumbnailContentType, cancellationToken);

            await mediaRepository.AddAsync(media.ToDao(), cancellationToken);
        }
        catch
        {
            await DeleteFilesAsync(media, CancellationToken.None);
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

        await DeleteFilesAsync(media.ToEntity(), cancellationToken);
        return true;
    }

    private async Task<MediaFile?> OpenAsync(string key, CancellationToken cancellationToken)
    {
        var asset = await assetStorage.OpenReadAsync(key, cancellationToken);
        return asset is null ? null : new MediaFile(asset.Content, asset.ContentType, asset.Length);
    }

    private async Task DeleteFilesAsync(MediaItem media, CancellationToken cancellationToken)
    {
        await assetStorage.DeleteAsync(media.ImagePath, cancellationToken);
        await assetStorage.DeleteAsync(media.ThumbnailPath, cancellationToken);
    }
}
