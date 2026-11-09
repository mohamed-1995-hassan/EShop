using EShop.Domain.Aggregates.CustomerAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly EShopDbContext _context;

    public CustomerProfileRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CustomerProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .FirstOrDefaultAsync(c => c.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<CustomerProfile?> GetWithAddressesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CustomerProfile?> GetWithWalletAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .Include(c => c.WalletTransactions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CustomerProfile?> GetWithLoyaltyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .Include(c => c.LoyaltyTransactions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CustomerProfile?> GetFullProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .Include(c => c.Addresses)
            .Include(c => c.WalletTransactions)
            .Include(c => c.LoyaltyTransactions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerProfile>> GetByTierAsync(CustomerTier tier, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .Where(c => c.Tier == tier)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerProfile>> GetTopCustomersAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .Include(c => c.LoyaltyTransactions)
            .OrderByDescending(c => c.TotalLifetimeSpend)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerProfile>> GetInactiveCustomersAsync(int daysSinceLastOrder, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastOrder);
        return await _context.Set<CustomerProfile>()
            .Where(c => c.LastOrderAt < cutoffDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CustomerProfile customer, CancellationToken cancellationToken = default)
    {
        await _context.Set<CustomerProfile>().AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CustomerProfile customer, CancellationToken cancellationToken = default)
    {
        _context.Set<CustomerProfile>().Update(customer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .AnyAsync(c => c.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<int> GetReferralCountAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CustomerProfile>()
            .CountAsync(c => c.ReferredByCustomerId == customerId, cancellationToken);
    }
}
