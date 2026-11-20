using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.Collections;

public interface ICollectionRepository
{
    Task<IReadOnlyList<CollectionDao>> GetCollectionsAsync(CollectionFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(CollectionDao collection, CancellationToken cancellationToken = default);

    Task UpdateAsync(CollectionDao collection, CancellationToken cancellationToken = default);

    /// <summary>Deletes the collection, its items and its detail page. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
