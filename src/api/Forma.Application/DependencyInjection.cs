using Forma.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWebsiteCoreService, WebsiteCoreService>();
        services.AddScoped<IPageCoreService, PageCoreService>();
        services.AddScoped<IMediaCoreService, MediaCoreService>();

        return services;
    }
}
