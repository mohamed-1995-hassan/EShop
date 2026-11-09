using EShop.Domain.Aggregates.SubscriptionAggregate;

namespace EShop.Domain.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByNumberAsync(string subscriptionNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetActiveByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetDueForBillingAsync(DateTime asOf, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetDueForDeliveryAsync(DateTime asOf, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetPausedExpiringSoonAsync(int daysAhead, CancellationToken cancellationToken = default);
    
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
    
    // Analytics
    Task<int> GetActiveSubscriptionCountAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetMonthlyRecurringRevenueAsync(CancellationToken cancellationToken = default);
}
