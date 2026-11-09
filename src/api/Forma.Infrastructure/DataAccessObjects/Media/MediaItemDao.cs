using Forma.Infrastructure.DataAccessObjects.Websites;

namespace Forma.Infrastructure.DataAccessObjects.Media;

/// <summary>An uploaded media file (currently images only) as it is stored in the database.</summary>
public class MediaItemDao
{
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeInBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Path of the original file, relative to the assets folder.</summary>
    public required string FilePath { get; set; }

    /// <summary>Path of the generated thumbnail, relative to the assets folder.</summary>
    public required string ThumbnailPath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WebsiteDao? Website { get; set; }
}
