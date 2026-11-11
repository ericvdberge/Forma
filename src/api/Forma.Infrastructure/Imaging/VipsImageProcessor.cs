using NetVips;

namespace Forma.Infrastructure.Imaging;

/// <summary>Validates images and renders WebP thumbnails with libvips (NetVips).</summary>
public class VipsImageProcessor : IImageProcessor
{
    /// <summary>Longest side of a thumbnail, in pixels. Smaller images keep their size.</summary>
    public const int ThumbnailMaxSize = 400;

    /// <summary>
    /// Most pixels an image may have (about 10,000 × 10,000). Compressed files can claim far larger sizes
    /// than their bytes suggest, and decoding those would take minutes and gigabytes.
    /// </summary>
    public const long MaxPixels = 100_000_000;

    private const string UnsupportedError = "Only JPEG, PNG, GIF and WebP images are allowed.";
    private const string DamagedError = "The image is damaged or incomplete.";

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

    public async Task<ImageProcessingResult> ProcessAsync(byte[] content, CancellationToken cancellationToken = default)
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

    private static ImageProcessingResult Process(byte[] content)
    {
        var loader = Image.FindLoadBuffer(content);
        if (loader is null || !SupportedLoaders.TryGetValue(loader, out var format))
        {
            return new ImageProcessingResult.Rejected(UnsupportedError);
        }

        try
        {
            // Opening reads only the header, so the size is checked before any pixels are decoded.
            // FailOn.Truncated makes decoding a cut-off or corrupt file fail instead of filling in grey.
            using var original = Image.NewFromBuffer(content, access: Enums.Access.Sequential, failOn: Enums.FailOn.Truncated);
            if ((long)original.Width * original.Height > MaxPixels)
            {
                return new ImageProcessingResult.Rejected(
                    $"The image is larger than {MaxPixels / 1_000_000} megapixels.");
            }

            // Decode every pixel once, top to bottom, so a damaged file is caught here. The thumbnail can't
            // tell: it decodes JPEGs at reduced size and only the first frame of a GIF.
            original.Avg();

            // Autorot applies the EXIF orientation, so width and height are as the image is displayed.
            using var oriented = original.Autorot();

            // ThumbnailBuffer also auto-rotates, and never upscales with Size.Down.
            using var thumbnail = Image.ThumbnailBuffer(content, ThumbnailMaxSize, height: ThumbnailMaxSize, size: Enums.Size.Down);

            var processed = new ProcessedImage(format.ContentType, format.Extension, oriented.Width, oriented.Height, thumbnail.WebpsaveBuffer(q: 80));
            return new ImageProcessingResult.Processed(processed);
        }
        catch (VipsException)
        {
            // The header matched a supported format but the image itself is corrupt.
            return new ImageProcessingResult.Rejected(DamagedError);
        }
    }
}
