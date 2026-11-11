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

        // Uploaded media are plain files under Assets:Path (relative paths resolve from the working directory).
        var assetsPath = configuration["Assets:Path"] is { Length: > 0 } path ? path : "assets";
        services.AddSingleton<IAssetStorage>(new FileSystemAssetStorage(assetsPath));
        services.AddSingleton<IImageProcessor, VipsImageProcessor>();

        return services;
    }
}
