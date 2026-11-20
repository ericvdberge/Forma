using Forma.Application.Entities;
using Forma.Application.Mappers;
using Forma.Infrastructure.Repositories.Pages;

namespace Forma.Application.Services;

/// <summary>Creates, reads, updates and deletes pages. Rules and workflows belong to the callers.</summary>
public interface IPageCoreService
{
    Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The first page matching <paramref name="filter"/>, or <c>null</c> when there is none.</summary>
    Task<Page?> GetPageAsync(PageFilter filter, CancellationToken cancellationToken = default);

    Task<Page> CreateAsync(PageInsert insert, CancellationToken cancellationToken = default);

    /// <summary>Applies <paramref name="update"/>, or returns <c>null</c> when the page does not exist.</summary>
    Task<Page?> UpdateAsync(Guid id, PageUpdate update, CancellationToken cancellationToken = default);

    /// <summary>Deletes the page and its content. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class PageCoreService(IPageRepository pageRepository, TimeProvider timeProvider) : IPageCoreService
{
    public async Task<IReadOnlyList<Page>> GetPagesAsync(PageFilter filter, CancellationToken cancellationToken = default)
    {
        var pages = await pageRepository.GetPagesAsync(filter, cancellationToken);
        return pages.ToEntities();
    }

    public async Task<Page?> GetPageAsync(PageFilter filter, CancellationToken cancellationToken = default)
    {
        var pages = await pageRepository.GetPagesAsync(filter, cancellationToken);
        return pages.FirstOrDefault()?.ToEntity();
    }

    public async Task<Page> CreateAsync(PageInsert insert, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var page = new Page
        {
            Id = Guid.CreateVersion7(),
            WebsiteId = insert.WebsiteId,
            Title = insert.Title,
            Slug = insert.Slug,
            Status = insert.Status,
            Kind = insert.Kind,
            CollectionId = insert.CollectionId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await pageRepository.AddAsync(page.ToDao(), cancellationToken);

        return page;
    }

    public async Task<Page?> UpdateAsync(Guid id, PageUpdate update, CancellationToken cancellationToken = default)
    {
        var page = await GetPageAsync(new PageFilter { Id = id }, cancellationToken);
        if (page is null)
            return null;

        page.Title = update.Title ?? page.Title;
        page.Slug = update.Slug ?? page.Slug;
        page.Status = update.Status ?? page.Status;
        page.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        await pageRepository.UpdateAsync(page.ToDao(), cancellationToken);

        return page;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await pageRepository.DeleteAsync(id, cancellationToken);
}
