namespace Forma.Infrastructure.Imaging;

public interface IImageProcessor
{
    /// <summary>
    /// Validates that <paramref name="content"/> is a supported image (JPEG, PNG, GIF or WebP,
    /// detected from the bytes, not the file name) that is complete and not too large, and renders its thumbnail.
    /// </summary>
    /// <remarks>Waits its turn when other images are being processed, so a bulk upload can't exhaust memory.</remarks>
    Task<ImageProcessingResult> ProcessAsync(byte[] content, CancellationToken cancellationToken = default);
}
