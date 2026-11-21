using Forma.Application.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Application.Seeding;

/// <summary>Fills <c>PageBindingUsages</c> from live content when it is empty, e.g. right after the migration that added it.</summary>
public static class PageBindingUsageSeeder
{
    public static async Task RebuildPageBindingUsagesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var usageService = scope.ServiceProvider.GetRequiredService<IPageBindingUsageService>();

        await usageService.RebuildAllIfEmptyAsync(cancellationToken);
    }
}
