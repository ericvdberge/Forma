namespace Forma.Application.Entities.Media;

/// <summary>An uploaded image in a website's media library.</summary>
public class MediaItem
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }

    /// <summary>The file name it was uploaded with.</summary>
    public required string FileName { get; set; }

    /// <summary>MIME type of the original, detected from its content.</summary>
    public required string ContentType { get; set; }

    public long SizeInBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Path of the original file, relative to the assets folder.</summary>
    public required string FilePath { get; set; }

    /// <summary>Path of the WebP thumbnail, relative to the assets folder.</summary>
    public required string ThumbnailPath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
