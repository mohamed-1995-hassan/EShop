using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

/// <summary>
/// Repository for Order aggregate.
/// 
/// DDD REPOSITORY RULES:
/// 1. One repository per aggregate root
/// 2. Repository loads/saves entire aggregate (including OrderLines)
/// 3. Repository is the only way to persist aggregates
/// 4. Queries should return complete aggregates
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly EShopDbContext _dbContext;

    public OrderRepository(EShopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Load entire aggregate including order lines
        return await _dbContext.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderLines)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderLines)
            .Where(o => o.Status == status)
            .OrderBy(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public void Update(Order order)
    {
        _dbContext.Orders.Update(order);
    }

    public void Remove(Order order)
    {
        _dbContext.Orders.Remove(order);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Save with outbox pattern - ensures events are stored atomically with entity changes
    /// </summary>
    public async Task<int> SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesWithOutboxAsync(cancellationToken);
    }
}
