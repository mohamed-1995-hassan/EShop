using EShop.Domain.Aggregates.PromotionAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly EShopDbContext _context;

    public PromotionRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Promotion>()
            .Include(p => p.Usages)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Promotion>()
            .Include(p => p.Usages)
            .FirstOrDefaultAsync(p => p.Code == code.ToUpperInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Promotion>()
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetByTypeAsync(PromotionType type, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Promotion>()
            .Where(p => p.Type == type)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetUpcomingPromotionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Promotion>()
            .Where(p => p.StartDate > now)
            .OrderBy(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetExpiredPromotionsAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Promotion>()
            .Where(p => p.EndDate < DateTime.UtcNow && p.EndDate >= since)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetPromotionsForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Promotion>()
            .Where(p => p.IsActive && 
                        p.StartDate <= now && 
                        p.EndDate >= now &&
                        (p.ApplicableProductIds.Count == 0 || p.ApplicableProductIds.Contains(productId)))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await _context.Set<Promotion>().AddAsync(promotion, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        _context.Set<Promotion>().Update(promotion);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Promotion>()
            .AnyAsync(p => p.Code == code.ToUpperInvariant(), cancellationToken);
    }

    public async Task<int> GetCustomerUsageCountAsync(Guid promotionId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Promotion>()
            .Where(p => p.Id == promotionId)
            .SelectMany(p => p.Usages)
            .CountAsync(u => u.CustomerId == customerId, cancellationToken);
    }
}
