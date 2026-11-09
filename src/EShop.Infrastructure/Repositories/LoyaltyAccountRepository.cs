using EShop.Domain.Aggregates.LoyaltyAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class LoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly EShopDbContext _context;

    public LoyaltyAccountRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<LoyaltyAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<LoyaltyAccount>()
            .Include(l => l.Transactions)
            .Include(l => l.AvailableRewards)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<LoyaltyAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<LoyaltyAccount>()
            .Include(l => l.Transactions.OrderByDescending(t => t.CreatedAt).Take(50))
            .FirstOrDefaultAsync(l => l.CustomerId == customerId, cancellationToken);
    }

    public async Task<LoyaltyAccount?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<LoyaltyAccount>()
            .FirstOrDefaultAsync(l => l.MembershipNumber == membershipNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<LoyaltyAccount>> GetByTierAsync(LoyaltyTier tier, CancellationToken cancellationToken = default)
    {
        return await _context.Set<LoyaltyAccount>()
            .Where(l => l.Tier == tier && l.IsActive)
            .OrderByDescending(l => l.LifetimePoints)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LoyaltyAccount>> GetAccountsWithExpiringPointsAsync(int withinDays, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTime.UtcNow.AddDays(withinDays);
        return await _context.Set<LoyaltyAccount>()
            .Include(l => l.Transactions)
            .Where(l => l.IsActive && 
                        l.Transactions.Any(t => t.ExpiresAt.HasValue && 
                                               t.ExpiresAt.Value <= expiryDate && 
                                               !t.IsExpired &&
                                               t.RemainingPoints > 0))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LoyaltyAccount>> GetTopMembersAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Set<LoyaltyAccount>()
            .Where(l => l.IsActive)
            .OrderByDescending(l => l.LifetimePoints)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<LoyaltyAccount>()
            .CountAsync(l => l.IsActive, cancellationToken);
    }

    public async Task<long> GetTotalOutstandingPointsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<LoyaltyAccount>()
            .Where(l => l.IsActive)
            .SumAsync(l => (long)l.CurrentPoints, cancellationToken);
    }

    public async Task AddAsync(LoyaltyAccount account, CancellationToken cancellationToken = default)
    {
        await _context.Set<LoyaltyAccount>().AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoyaltyAccount account, CancellationToken cancellationToken = default)
    {
        _context.Set<LoyaltyAccount>().Update(account);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
