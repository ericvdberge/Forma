namespace Forma.Infrastructure.Imaging;

/// <summary>Outcome of <see cref="IImageProcessor.ProcessAsync"/>.</summary>
public abstract record ImageProcessingResult
{
    private ImageProcessingResult() { }

    /// <summary>The image is supported and intact.</summary>
    public sealed record Processed(ProcessedImage Image) : ImageProcessingResult;

    /// <summary>The file can't be used; <paramref name="Error"/> says why, for the person who uploaded it.</summary>
    public sealed record Rejected(string Error) : ImageProcessingResult;
}
