using Amazon.Runtime;
using Amazon.S3;
using Forma.Infrastructure.Imaging;
using Forma.Infrastructure.Persistence;
using Forma.Infrastructure.Repositories.Media;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Infrastructure.Repositories.Websites;
using Forma.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Forma")
            ?? throw new InvalidOperationException("Connection string 'Forma' is not configured.");

        services.AddDbContext<FormaDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IWebsiteRepository, WebsiteRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();

        services.AddSingleton(_ => CreateStorageOptions(configuration));
        services.AddSingleton<IAmazonS3>(sp => CreateS3Client(sp.GetRequiredService<S3StorageOptions>()));
        services.AddSingleton<IAssetStorage, S3AssetStorage>();
        services.AddSingleton<IImageProcessor, VipsImageProcessor>();

        return services;
    }

    private static S3StorageOptions CreateStorageOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(S3StorageOptions.SectionName);
        string Required(string key) => section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"'{S3StorageOptions.SectionName}:{key}' is not configured.");

        return new S3StorageOptions
        {
            ServiceUrl = Required(nameof(S3StorageOptions.ServiceUrl)),
            Bucket = Required(nameof(S3StorageOptions.Bucket)),
            AccessKey = Required(nameof(S3StorageOptions.AccessKey)),
            SecretKey = Required(nameof(S3StorageOptions.SecretKey)),
            Region = section[nameof(S3StorageOptions.Region)] ?? "us-east-1",
        };
    }

    private static AmazonS3Client CreateS3Client(S3StorageOptions options) =>
        new(new BasicAWSCredentials(options.AccessKey, options.SecretKey), new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            AuthenticationRegion = options.Region,
            // S3-compatible stores address buckets by path, not by subdomain.
            ForcePathStyle = true,
            // Only send/verify checksums when S3 requires them; not every S3-compatible store supports the newer ones.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        });
}
