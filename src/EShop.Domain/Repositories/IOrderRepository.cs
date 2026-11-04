using EShop.Domain.Aggregates.OrderAggregate;

namespace EShop.Domain.Repositories;

/// <summary>
/// Repository interface - defined in Domain layer.
/// 
/// KEY POINTS:
/// - One repository per aggregate root
/// - Interface is in Domain layer
/// - Implementation is in Infrastructure layer
/// - Returns aggregates, not DTOs
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task DeleteAsync(Order order, CancellationToken cancellationToken = default);
}
