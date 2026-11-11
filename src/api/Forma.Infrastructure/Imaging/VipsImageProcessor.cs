using NetVips;

namespace Forma.Infrastructure.Imaging;

/// <summary>Validates images and renders WebP thumbnails with libvips (NetVips).</summary>
public class VipsImageProcessor : IImageProcessor
{
    /// <summary>Longest side of a thumbnail, in pixels. Smaller images keep their size.</summary>
    public const int ThumbnailMaxSize = 400;

    /// <summary>
    /// How many images are decoded at once. Each decode holds the upload and its pixel buffers, and libvips
    /// already spreads one image over every core, so a bulk upload queues here instead of running out of memory.
    /// </summary>
    private static readonly int MaxConcurrentImages = Math.Clamp(Environment.ProcessorCount / 2, 1, 4);

    private readonly SemaphoreSlim _gate = new(MaxConcurrentImages, MaxConcurrentImages);

    static VipsImageProcessor()
    {
        // Every upload is processed once, so libvips' operation cache never gets a hit. It would only keep
        // recent uploads, up to 10 MB each, alive in native memory.
        Cache.Max = 0;
    }

    // libvips names the loader that recognises the bytes; only these formats are accepted.
    private static readonly Dictionary<string, (string ContentType, string Extension)> SupportedLoaders = new()
    {
        ["VipsForeignLoadJpegBuffer"] = ("image/jpeg", ".jpg"),
        ["VipsForeignLoadPngBuffer"] = ("image/png", ".png"),
        ["VipsForeignLoadNsgifBuffer"] = ("image/gif", ".gif"),
        ["VipsForeignLoadWebpBuffer"] = ("image/webp", ".webp"),
    };

    public async Task<ProcessedImage?> ProcessAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return Process(content);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static ProcessedImage? Process(byte[] content)
    {
        var loader = Image.FindLoadBuffer(content);
        if (loader is null || !SupportedLoaders.TryGetValue(loader, out var format))
        {
            return null;
        }

        try
        {
            // Autorot applies the EXIF orientation, so width and height are as the image is displayed.
            using var original = Image.NewFromBuffer(content, access: Enums.Access.Sequential);
            using var oriented = original.Autorot();

            // ThumbnailBuffer also auto-rotates, and never upscales with Size.Down.
            using var thumbnail = Image.ThumbnailBuffer(content, ThumbnailMaxSize, height: ThumbnailMaxSize, size: Enums.Size.Down);

            return new ProcessedImage(format.ContentType, format.Extension, oriented.Width, oriented.Height, thumbnail.WebpsaveBuffer(q: 80));
        }
        catch (VipsException)
        {
            // The header matched a supported format but the image itself is corrupt.
            return null;
        }
    }
}
