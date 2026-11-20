using Forma.Application.Entities;
using Forma.Application.Mappers;
using Forma.Infrastructure.Repositories.Collections;

namespace Forma.Application.Services;

/// <summary>Creates, reads, updates and deletes collections. Rules and workflows belong to the callers.</summary>
public interface ICollectionCoreService
{
    Task<IReadOnlyList<Collection>> GetCollectionsAsync(CollectionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The first collection matching <paramref name="filter"/>, or <c>null</c> when there is none.</summary>
    Task<Collection?> GetCollectionAsync(CollectionFilter filter, CancellationToken cancellationToken = default);

    Task<Collection> CreateAsync(CollectionInsert insert, CancellationToken cancellationToken = default);

    /// <summary>Applies <paramref name="update"/>, or returns <c>null</c> when the collection does not exist.</summary>
    Task<Collection?> UpdateAsync(Guid id, CollectionUpdate update, CancellationToken cancellationToken = default);

    /// <summary>Deletes the collection, its items and its detail page. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CollectionCoreService(ICollectionRepository collectionRepository, TimeProvider timeProvider) : ICollectionCoreService
{
    public async Task<IReadOnlyList<Collection>> GetCollectionsAsync(CollectionFilter filter, CancellationToken cancellationToken = default)
    {
        var collections = await collectionRepository.GetCollectionsAsync(filter, cancellationToken);
        return collections.ToEntities();
    }

    public async Task<Collection?> GetCollectionAsync(CollectionFilter filter, CancellationToken cancellationToken = default)
    {
        var collections = await collectionRepository.GetCollectionsAsync(filter, cancellationToken);
        return collections.FirstOrDefault()?.ToEntity();
    }

    public async Task<Collection> CreateAsync(CollectionInsert insert, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var collection = new Collection
        {
            Id = Guid.CreateVersion7(),
            WebsiteId = insert.WebsiteId,
            Name = insert.Name,
            SingularName = insert.SingularName,
            Slug = insert.Slug,
            Fields = insert.Fields,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await collectionRepository.AddAsync(collection.ToDao(), cancellationToken);

        return collection;
    }

    public async Task<Collection?> UpdateAsync(Guid id, CollectionUpdate update, CancellationToken cancellationToken = default)
    {
        var collection = await GetCollectionAsync(new CollectionFilter { Id = id }, cancellationToken);
        if (collection is null)
            return null;

        collection.Name = update.Name ?? collection.Name;
        collection.SingularName = update.SingularName ?? collection.SingularName;
        collection.Slug = update.Slug ?? collection.Slug;
        collection.Fields = update.Fields ?? collection.Fields;
        collection.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        await collectionRepository.UpdateAsync(collection.ToDao(), cancellationToken);

        return collection;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await collectionRepository.DeleteAsync(id, cancellationToken);
}
