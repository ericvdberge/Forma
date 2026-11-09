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

        services.AddSingleton(CreateAssetStorageOptions(configuration));
        services.AddSingleton<IAssetStorage, LocalAssetStorage>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();

        return services;
    }

    private static AssetStorageOptions CreateAssetStorageOptions(IConfiguration configuration)
    {
        var path = configuration[$"{AssetStorageOptions.SectionName}:Path"];
        if (string.IsNullOrWhiteSpace(path))
        {
            path = "assets";
        }

        // Relative paths are resolved against the host's content root (the project folder in development).
        var contentRoot = configuration["contentRoot"] ?? Directory.GetCurrentDirectory();
        return new AssetStorageOptions { RootPath = Path.GetFullPath(path, contentRoot) };
    }
}
