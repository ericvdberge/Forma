namespace Forma.Api.Dtos;

/// <summary>An image in a website's media library.</summary>
/// <param name="Id">The media item's id.</param>
/// <param name="WebsiteId">The website the media item belongs to.</param>
/// <param name="FileName">The file name the image was uploaded with.</param>
/// <param name="ContentType">The original image's MIME type, for example <c>image/png</c>.</param>
/// <param name="SizeInBytes">The original image's size in bytes.</param>
/// <param name="Width">The original image's width in pixels.</param>
/// <param name="Height">The original image's height in pixels.</param>
/// <param name="ImageUrl">Download route of the original image, relative to the API's base URL.</param>
/// <param name="ThumbnailUrl">Download route of the WebP thumbnail, relative to the API's base URL.</param>
/// <param name="CreatedAt">When the image was uploaded (UTC).</param>
/// <param name="UpdatedAt">When the media item was last changed (UTC).</param>
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
