using Forma.Application.Entities;
using Forma.Application.Mappers;
using Forma.Infrastructure.Repositories.CollectionItems;

namespace Forma.Application.Services;

/// <summary>Creates, reads, updates and deletes collection items. Rules and workflows belong to the callers.</summary>
public interface ICollectionItemCoreService
{
    Task<IReadOnlyList<CollectionItem>> GetCollectionItemsAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The first item matching <paramref name="filter"/>, or <c>null</c> when there is none.</summary>
    Task<CollectionItem?> GetCollectionItemAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default);

    Task<CollectionItem> CreateAsync(CollectionItemInsert insert, CancellationToken cancellationToken = default);

    /// <summary>Applies <paramref name="update"/>, or returns <c>null</c> when the item does not exist.</summary>
    Task<CollectionItem?> UpdateAsync(Guid id, CollectionItemUpdate update, CancellationToken cancellationToken = default);

    /// <summary>Deletes the item. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CollectionItemCoreService(ICollectionItemRepository itemRepository, TimeProvider timeProvider) : ICollectionItemCoreService
{
    public async Task<IReadOnlyList<CollectionItem>> GetCollectionItemsAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default)
    {
        var items = await itemRepository.GetCollectionItemsAsync(filter, cancellationToken);
        return items.ToEntities();
    }

    public async Task<CollectionItem?> GetCollectionItemAsync(CollectionItemFilter filter, CancellationToken cancellationToken = default)
    {
        var items = await itemRepository.GetCollectionItemsAsync(filter, cancellationToken);
        return items.FirstOrDefault()?.ToEntity();
    }

    public async Task<CollectionItem> CreateAsync(CollectionItemInsert insert, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var item = new CollectionItem
        {
            Id = Guid.CreateVersion7(),
            CollectionId = insert.CollectionId,
            Slug = insert.Slug,
            Status = insert.Status,
            Values = insert.Values,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = insert.Status == CollectionItemStatus.Published ? now : null,
        };

        await itemRepository.AddAsync(item.ToDao(), cancellationToken);

        return item;
    }

    public async Task<CollectionItem?> UpdateAsync(Guid id, CollectionItemUpdate update, CancellationToken cancellationToken = default)
    {
        var item = await GetCollectionItemAsync(new CollectionItemFilter { Id = id }, cancellationToken);
        if (item is null)
            return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        item.Slug = update.Slug ?? item.Slug;
        item.Values = update.Values ?? item.Values;
        item.Status = update.Status ?? item.Status;
        item.UpdatedAt = now;

        if (update.Status == CollectionItemStatus.Published)
            item.PublishedAt = now;

        await itemRepository.UpdateAsync(item.ToDao(), cancellationToken);

        return item;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await itemRepository.DeleteAsync(id, cancellationToken);
}
