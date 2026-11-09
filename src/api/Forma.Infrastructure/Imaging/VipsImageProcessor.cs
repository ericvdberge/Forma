using NetVips;

namespace Forma.Infrastructure.Imaging;

/// <summary>Validates images and renders WebP thumbnails with libvips (NetVips).</summary>
public class VipsImageProcessor : IImageProcessor
{
    /// <summary>Longest side of a thumbnail, in pixels. Smaller images keep their size.</summary>
    public const int ThumbnailMaxSize = 400;

    // libvips names the loader that recognises the bytes; only these formats are accepted.
    private static readonly Dictionary<string, (string ContentType, string Extension)> SupportedLoaders = new()
    {
        ["VipsForeignLoadJpegBuffer"] = ("image/jpeg", ".jpg"),
        ["VipsForeignLoadPngBuffer"] = ("image/png", ".png"),
        ["VipsForeignLoadNsgifBuffer"] = ("image/gif", ".gif"),
        ["VipsForeignLoadWebpBuffer"] = ("image/webp", ".webp"),
    };

    public ProcessedImage? Process(byte[] content)
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
