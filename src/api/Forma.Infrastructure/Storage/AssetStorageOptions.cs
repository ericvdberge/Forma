namespace Forma.Infrastructure.Storage;

/// <summary>Where uploaded files are written. Bound from the <c>Assets</c> configuration section.</summary>
public class AssetStorageOptions
{
    public const string SectionName = "Assets";

    /// <summary>
    /// Absolute path of the assets folder. A relative <c>Assets:Path</c> setting is resolved
    /// against the content root; in the Docker image this is <c>/app/assets</c>, a volume.
    /// </summary>
    public required string RootPath { get; init; }
}
