namespace Forma.Api.Dtos.Media;

/// <param name="ImageUrl">Download route of the original image, relative to the API's base URL.</param>
/// <param name="ThumbnailUrl">Download route of the WebP thumbnail, relative to the API's base URL.</param>
public record MediaItemDto(
    Guid Id,
    Guid WebsiteId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    int Width,
    int Height,
    string ImageUrl,
    string ThumbnailUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);
