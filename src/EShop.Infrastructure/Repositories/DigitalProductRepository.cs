using EShop.Domain.Aggregates.DigitalProductAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class DigitalProductRepository : IDigitalProductRepository
{
    private readonly EShopDbContext _context;

    public DigitalProductRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<DigitalProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DigitalProduct>()
            .Include(d => d.Files)
            .Include(d => d.LicenseKeys)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DigitalProduct?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DigitalProduct>()
            .Include(d => d.Files)
            .Include(d => d.LicenseKeys)
            .FirstOrDefaultAsync(d => d.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<DigitalProduct>> GetByTypeAsync(DigitalProductType type, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DigitalProduct>()
            .Where(d => d.Type == type && d.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DigitalProduct>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<DigitalProduct>()
            .Include(d => d.Files.Where(f => f.IsActive))
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<LicenseKey?> GetLicenseByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DigitalProduct>()
            .SelectMany(d => d.LicenseKeys)
            .FirstOrDefaultAsync(l => l.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<LicenseKey>> GetLicensesByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DigitalProduct>()
            .SelectMany(d => d.LicenseKeys)
            .Where(l => l.AssignedToCustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DigitalProduct product, CancellationToken cancellationToken = default)
    {
        await _context.Set<DigitalProduct>().AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DigitalProduct product, CancellationToken cancellationToken = default)
    {
        _context.Set<DigitalProduct>().Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
