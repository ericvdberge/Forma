using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forma.Infrastructure.Repositories.Media;

public class MediaRepository(FormaDbContext db) : IMediaRepository
{
    public async Task<IReadOnlyList<MediaItemDao>> GetMediaItemsAsync(MediaFilter filter, CancellationToken cancellationToken = default) =>
        await db.Media
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<MediaItemDao?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Media
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AddAsync(MediaItemDao media, CancellationToken cancellationToken = default)
    {
        db.Media.Add(media);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Media.Where(m => m.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
