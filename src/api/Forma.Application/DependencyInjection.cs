using Forma.Application.Services.Websites;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWebsiteCoreService, WebsiteCoreService>();

        return services;
    }
}
