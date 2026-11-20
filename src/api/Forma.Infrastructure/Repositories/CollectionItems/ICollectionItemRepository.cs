using Forma.Infrastructure.Daos;

namespace Forma.Infrastructure.Repositories.CollectionItems;

public interface ICollectionItemRepository
{
    Task<IReadOnlyList<CollectionItemDao>> GetCollectionItemsAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default);

    Task AddAsync(CollectionItemDao item, CancellationToken cancellationToken = default);

    Task UpdateAsync(CollectionItemDao item, CancellationToken cancellationToken = default);

    /// <summary>Deletes the item. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
