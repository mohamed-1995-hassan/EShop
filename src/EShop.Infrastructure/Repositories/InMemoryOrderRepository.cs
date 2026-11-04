using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

/// <summary>
/// Repository implementation - in Infrastructure layer.
/// Uses in-memory storage for demo purposes.
/// 
/// In real applications, this would use:
/// - Entity Framework Core
/// - Dapper
/// - MongoDB driver
/// - etc.
/// </summary>
public class InMemoryOrderRepository : IOrderRepository
{
    // Thread-safe dictionary for concurrent access
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();
    
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }
    
    public Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = _orders.Values.Where(o => o.CustomerId == customerId);
        return Task.FromResult(orders);
    }
    
    public Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Order>>(_orders.Values.ToList());
    }
    
    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
    
    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders.TryRemove(order.Id, out _);
        return Task.CompletedTask;
    }
}
