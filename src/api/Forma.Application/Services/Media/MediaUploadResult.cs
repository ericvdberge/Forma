using Forma.Application.Entities.Media;

namespace Forma.Application.Services.Media;

/// <summary>Outcome of <see cref="IMediaCoreService.UploadAsync"/>.</summary>
public abstract record MediaUploadResult
{
    private MediaUploadResult() { }

    public sealed record Uploaded(MediaItem Media) : MediaUploadResult;

    public sealed record WebsiteNotFound : MediaUploadResult;

    /// <summary>The file was rejected; <paramref name="Error"/> says why.</summary>
    public sealed record Rejected(string Error) : MediaUploadResult;
}
