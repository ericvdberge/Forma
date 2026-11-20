using Forma.Infrastructure.Repositories.Collections;

namespace Forma.Application.Services;

/// <summary>
/// The rules for a collection that need the database, on top of the request validator's own checks.
/// Errors are keyed by property, ready for a <c>400</c> validation problem.
/// </summary>
public interface ICollectionValidationService
{
    /// <summary>
    /// The errors of giving a collection of <paramref name="websiteId"/> the slug <paramref name="slug"/>;
    /// <paramref name="collectionId"/> is the collection being changed, or <c>null</c> for a new one. Empty when there are none.
    /// </summary>
    Task<IDictionary<string, string[]>> ValidateAsync(Guid websiteId, string slug, Guid? collectionId, CancellationToken cancellationToken = default);
}

public class CollectionValidationService(ICollectionCoreService collectionCoreService) : ICollectionValidationService
{
    public async Task<IDictionary<string, string[]>> ValidateAsync(Guid websiteId, string slug, Guid? collectionId, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        var existing = await collectionCoreService.GetCollectionAsync(new CollectionFilter { WebsiteId = websiteId, Slug = slug }, cancellationToken);

        if (existing is not null && existing.Id != collectionId)
            errors["slug"] = [$"The collection ‘{existing.Name}’ already uses /{slug}."];

        return errors;
    }
}
