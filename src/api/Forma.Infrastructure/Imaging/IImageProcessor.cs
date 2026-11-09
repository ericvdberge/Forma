namespace Forma.Infrastructure.Imaging;

public interface IImageProcessor
{
    /// <summary>
    /// Validates that <paramref name="content"/> is a supported image (JPEG, PNG, GIF or WebP,
    /// detected from the bytes, not the file name) and renders its thumbnail.
    /// Returns <c>null</c> when it is not a supported image.
    /// </summary>
    ProcessedImage? Process(Stream content);
}
