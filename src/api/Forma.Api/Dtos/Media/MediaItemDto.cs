namespace Forma.Api.Dtos.Media;

/// <param name="Url">Where the original is served, relative to the API's base URL.</param>
/// <param name="ThumbnailUrl">Where the WebP thumbnail is served, relative to the API's base URL.</param>
public record MediaItemDto(
    Guid Id,
    Guid WebsiteId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    int Width,
    int Height,
    string Url,
    string ThumbnailUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);
