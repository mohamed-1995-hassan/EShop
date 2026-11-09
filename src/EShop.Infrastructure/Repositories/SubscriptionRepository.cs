using EShop.Domain.Aggregates.SubscriptionAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly EShopDbContext _context;

    public SubscriptionRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Items)
            .Include(s => s.Deliveries)
            .Include(s => s.StatusHistory)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Subscription?> GetByNumberAsync(string subscriptionNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Items)
            .Include(s => s.Deliveries)
            .FirstOrDefaultAsync(s => s.SubscriptionNumber == subscriptionNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Items)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Items)
            .Where(s => s.CustomerId == customerId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Where(s => s.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetDueForBillingAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Items)
            .Where(s => s.Status == SubscriptionStatus.Active && 
                        s.NextBillingDate.HasValue && 
                        s.NextBillingDate.Value.Date <= asOf.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetDueForDeliveryAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Items)
            .Where(s => s.Status == SubscriptionStatus.Active && 
                        s.NextDeliveryDate.HasValue && 
                        s.NextDeliveryDate.Value.Date <= asOf.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetPausedExpiringSoonAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Set<Subscription>()
            .Where(s => s.Status == SubscriptionStatus.Paused &&
                        s.PausedUntil.HasValue &&
                        s.PausedUntil.Value <= expiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveSubscriptionCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Subscription>()
            .CountAsync(s => s.Status == SubscriptionStatus.Active, cancellationToken);
    }

    public async Task<decimal> GetMonthlyRecurringRevenueAsync(CancellationToken cancellationToken = default)
    {
        // Calculate MRR by summing monthly-equivalent prices of all active subscriptions
        var subscriptions = await _context.Set<Subscription>()
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);

        return subscriptions.Sum(s => s.BillingCycle switch
        {
            BillingCycle.Weekly => s.TotalPrice.Amount * 4.33m, // Convert to monthly
            BillingCycle.BiWeekly => s.TotalPrice.Amount * 2.17m,
            BillingCycle.Monthly => s.TotalPrice.Amount,
            BillingCycle.Quarterly => s.TotalPrice.Amount / 3,
            BillingCycle.Yearly => s.TotalPrice.Amount / 12,
            _ => s.TotalPrice.Amount
        });
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Set<Subscription>().AddAsync(subscription, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _context.Set<Subscription>().Update(subscription);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
