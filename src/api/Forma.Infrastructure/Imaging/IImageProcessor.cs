namespace Forma.Infrastructure.Imaging;

public interface IImageProcessor
{
    /// <summary>
    /// Validates that <paramref name="content"/> is a supported image (JPEG, PNG, GIF or WebP,
    /// detected from the bytes, not the file name) and renders its thumbnail.
    /// Returns <c>null</c> when it is not a supported image.
    /// </summary>
    /// <remarks>Waits its turn when other images are being processed, so a bulk upload can't exhaust memory.</remarks>
    Task<ProcessedImage?> ProcessAsync(byte[] content, CancellationToken cancellationToken = default);
}
