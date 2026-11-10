using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace Forma.Infrastructure.Storage;

/// <summary>Keeps assets in one bucket of an S3-compatible store.</summary>
public class S3AssetStorage(IAmazonS3 s3, S3StorageOptions options) : IAssetStorage
{
    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        try
        {
            await s3.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception e) when (e.ErrorCode == "NoSuchBucket")
        {
            // First upload against a fresh store: create the bucket and try once more.
            try
            {
                await s3.PutBucketAsync(options.Bucket, cancellationToken);
            }
            catch (AmazonS3Exception created) when (created.ErrorCode == "BucketAlreadyOwnedByYou")
            {
                // Uploads that run side by side race to create it; losing that race is fine.
            }

            content.Position = 0;
            await s3.PutObjectAsync(request, cancellationToken);
        }
    }

    public async Task<StoredAsset?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await s3.GetObjectAsync(options.Bucket, key, cancellationToken);
            return new StoredAsset(response.ResponseStream, response.Headers.ContentType, response.ContentLength);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        await s3.DeleteObjectAsync(options.Bucket, key, cancellationToken);
}
