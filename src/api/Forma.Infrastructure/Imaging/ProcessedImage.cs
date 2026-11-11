namespace Forma.Infrastructure.Imaging;

/// <summary>A validated image and its thumbnail.</summary>
/// <param name="ContentType">MIME type of the original, detected from its content.</param>
/// <param name="FileExtension">Extension for the original, including the dot.</param>
/// <param name="Width">Width of the original, after applying its EXIF orientation.</param>
/// <param name="Height">Height of the original, after applying its EXIF orientation.</param>
/// <param name="Thumbnail">The thumbnail, encoded as WebP.</param>
public record ProcessedImage(string ContentType, string FileExtension, int Width, int Height, byte[] Thumbnail)
{
    public const string ThumbnailContentType = "image/webp";
    public const string ThumbnailFileExtension = ".webp";
}
