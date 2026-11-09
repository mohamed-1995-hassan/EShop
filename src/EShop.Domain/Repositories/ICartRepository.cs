using EShop.Domain.Aggregates.CartAggregate;

namespace EShop.Domain.Repositories;

public interface ICartRepository
{
    Task<ShoppingCart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetActiveCartAsync(Guid? customerId, string sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShoppingCart>> GetAbandonedCartsAsync(TimeSpan abandonedAfter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShoppingCart>> GetExpiredCartsAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Analytics
    Task<int> GetAbandonedCartCountAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<decimal> GetAbandonedCartValueAsync(DateTime since, CancellationToken cancellationToken = default);
}
