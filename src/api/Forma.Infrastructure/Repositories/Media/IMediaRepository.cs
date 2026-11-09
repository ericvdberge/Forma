using Forma.Infrastructure.DataAccessObjects.Media;

namespace Forma.Infrastructure.Repositories.Media;

public interface IMediaRepository
{
    Task<IReadOnlyList<MediaItemDao>> GetMediaAsync(MediaFilter filter, CancellationToken cancellationToken = default);

    Task<MediaItemDao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MediaItemDao media, CancellationToken cancellationToken = default);

    /// <summary>Deletes the media item. Returns <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
