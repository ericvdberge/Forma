using SkiaSharp;

namespace Forma.Infrastructure.Imaging;

/// <summary>Validates images and renders WebP thumbnails with SkiaSharp.</summary>
public class SkiaImageProcessor : IImageProcessor
{
    /// <summary>Longest side of a thumbnail, in pixels. Smaller images keep their size.</summary>
    public const int ThumbnailMaxSize = 400;

    private const int ThumbnailQuality = 80;

    /// <summary>Guards against decompression bombs: a small file that decodes to a huge bitmap.</summary>
    private const long MaxPixelCount = 50_000_000;

    private static readonly Dictionary<SKEncodedImageFormat, (string ContentType, string Extension)> SupportedFormats = new()
    {
        [SKEncodedImageFormat.Jpeg] = ("image/jpeg", ".jpg"),
        [SKEncodedImageFormat.Png] = ("image/png", ".png"),
        [SKEncodedImageFormat.Gif] = ("image/gif", ".gif"),
        [SKEncodedImageFormat.Webp] = ("image/webp", ".webp"),
    };

    public ProcessedImage? Process(Stream content)
    {
        using var data = SKData.Create(content);
        if (data is null)
        {
            return null;
        }

        using var codec = SKCodec.Create(data);
        if (codec is null || !SupportedFormats.TryGetValue(codec.EncodedFormat, out var format))
        {
            return null;
        }

        var rawWidth = codec.Info.Width;
        var rawHeight = codec.Info.Height;
        if (rawWidth <= 0 || rawHeight <= 0 || (long)rawWidth * rawHeight > MaxPixelCount)
        {
            return null;
        }

        // Decodes the first frame; animated GIF/WebP get a still thumbnail.
        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
        {
            return null;
        }

        var origin = codec.EncodedOrigin;
        var swapsAxes = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swapsAxes ? rawHeight : rawWidth;
        var height = swapsAxes ? rawWidth : rawHeight;

        var thumbnail = RenderThumbnail(bitmap, origin, width, height);
        return new ProcessedImage(format.ContentType, format.Extension, width, height, thumbnail);
    }

    private static byte[] RenderThumbnail(SKBitmap bitmap, SKEncodedOrigin origin, int width, int height)
    {
        var scale = Math.Min(1d, (double)ThumbnailMaxSize / Math.Max(width, height));
        var thumbWidth = Math.Max(1, (int)Math.Round(width * scale));
        var thumbHeight = Math.Max(1, (int)Math.Round(height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(thumbWidth, thumbHeight, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Could not allocate a thumbnail surface.");
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Draw the raw (unrotated) image scaled down, then let the matrix apply the EXIF orientation.
        var orientation = OrientationMatrix(origin, thumbWidth, thumbHeight);
        canvas.SetMatrix(in orientation);
        var swapsAxes = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var destination = swapsAxes
            ? SKRect.Create(thumbHeight, thumbWidth)
            : SKRect.Create(thumbWidth, thumbHeight);

        using var image = SKImage.FromBitmap(bitmap);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawImage(image, destination, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
        canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Webp, ThumbnailQuality)
            ?? throw new InvalidOperationException("Could not encode the thumbnail.");
        return encoded.ToArray();
    }

    /// <summary>Maps a point of the raw image onto the oriented thumbnail of size <paramref name="w"/>×<paramref name="h"/>.</summary>
    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, float w, float h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, w, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, w, -1, 0, h, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, h, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
