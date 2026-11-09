using EShop.Domain.Aggregates.BundleAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class BundleRepository : IBundleRepository
{
    private readonly EShopDbContext _context;

    public BundleRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Bundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Bundle>()
            .Include(b => b.Items)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Bundle?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Bundle>()
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Sku == sku, cancellationToken);
    }

    public async Task<IReadOnlyList<Bundle>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Bundle>()
            .Include(b => b.Items)
            .Where(b => b.Status == BundleStatus.Active &&
                        (!b.ValidFrom.HasValue || b.ValidFrom.Value <= now) &&
                        (!b.ValidUntil.HasValue || b.ValidUntil.Value >= now))
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Bundle>> GetByTypeAsync(BundleType type, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Bundle>()
            .Include(b => b.Items)
            .Where(b => b.Type == type && b.Status == BundleStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Bundle>> GetByStatusAsync(BundleStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Bundle>()
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Bundle>> GetBundlesContainingProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Bundle>()
            .Include(b => b.Items)
            .Where(b => b.Status == BundleStatus.Active &&
                        b.Items.Any(i => i.ProductId == productId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Bundle>> GetExpiringAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Bundle>()
            .Where(b => b.Status == BundleStatus.Active &&
                        b.ValidUntil.HasValue &&
                        b.ValidUntil.Value <= beforeDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Bundle bundle, CancellationToken cancellationToken = default)
    {
        await _context.Set<Bundle>().AddAsync(bundle, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Bundle bundle, CancellationToken cancellationToken = default)
    {
        _context.Set<Bundle>().Update(bundle);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bundle = await GetByIdAsync(id, cancellationToken);
        if (bundle != null)
        {
            _context.Set<Bundle>().Remove(bundle);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
