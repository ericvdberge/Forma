namespace Forma.Api.Abstractions;

/// <summary>
/// Implemented once per entity. Every implementation in this assembly is
/// discovered and mapped automatically by <see cref="EndpointExtensions.MapEndpoints"/>.
/// </summary>
public interface IEndpoints
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
