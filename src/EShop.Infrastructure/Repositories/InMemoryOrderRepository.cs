using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

/// <summary>
/// Repository implementation - in Infrastructure layer.
/// Uses in-memory storage for demo/testing purposes.
/// 
/// NOTE: For production, use OrderRepository with EF Core.
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
    
    public Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = _orders.Values
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Order>>(orders);
    }
    
    public Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        var orders = _orders.Values
            .Where(o => o.Status == status)
            .OrderBy(o => o.OrderDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Order>>(orders);
    }
    
    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
    
    public void Update(Order order)
    {
        _orders[order.Id] = order;
    }
    
    public void Remove(Order order)
    {
        _orders.TryRemove(order.Id, out _);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // In-memory: changes are immediate
        return Task.FromResult(1);
    }

    public Task<int> SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
    {
        // In-memory: no outbox needed
        return Task.FromResult(1);
    }
}
