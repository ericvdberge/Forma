using System.Reflection;

namespace Forma.Api.Abstractions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpoints).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            var endpoints = (IEndpoints)Activator.CreateInstance(type)!;
            endpoints.MapEndpoints(app);
        }

        return app;
    }
}
