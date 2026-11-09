namespace Forma.Infrastructure.Storage;

/// <summary>
/// The S3-compatible store that holds uploaded media, bound from the <c>Storage</c> configuration section.
/// Locally (and in the Docker image) this is versitygw, which keeps every object as a plain file on disk.
/// </summary>
public class S3StorageOptions
{
    public const string SectionName = "Storage";

    public required string ServiceUrl { get; init; }
    public required string Bucket { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public string Region { get; init; } = "us-east-1";
}
