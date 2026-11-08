using Forma.Infrastructure.Persistence;
using Forma.Infrastructure.Repositories.Pages;
using Forma.Infrastructure.Repositories.Websites;
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

        return services;
    }
}
