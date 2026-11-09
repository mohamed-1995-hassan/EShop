using EShop.Domain.Aggregates.CartAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly EShopDbContext _context;

    public CartRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<ShoppingCart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ShoppingCart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ShoppingCart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ShoppingCart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == CartStatus.Active, cancellationToken);
    }

    public async Task<ShoppingCart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ShoppingCart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.Status == CartStatus.Active, cancellationToken);
    }

    public async Task<ShoppingCart?> GetActiveCartAsync(Guid? customerId, string sessionId, CancellationToken cancellationToken = default)
    {
        if (customerId.HasValue)
        {
            var customerCart = await GetByCustomerIdAsync(customerId.Value, cancellationToken);
            if (customerCart != null) return customerCart;
        }
        
        return await GetBySessionIdAsync(sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingCart>> GetAbandonedCartsAsync(TimeSpan abandonedAfter, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - abandonedAfter;
        return await _context.Set<ShoppingCart>()
            .Where(c => c.Status == CartStatus.Active && c.LastActivityAt < cutoffDate)
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingCart>> GetExpiredCartsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ShoppingCart>()
            .Where(c => c.Status == CartStatus.Expired)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        await _context.Set<ShoppingCart>().AddAsync(cart, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        _context.Set<ShoppingCart>().Update(cart);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Set<ShoppingCart>().FindAsync(new object[] { id }, cancellationToken);
        if (cart != null)
        {
            _context.Set<ShoppingCart>().Remove(cart);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetAbandonedCartCountAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ShoppingCart>()
            .CountAsync(c => c.Status == CartStatus.Abandoned && c.LastActivityAt >= since, cancellationToken);
    }

    public async Task<decimal> GetAbandonedCartValueAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        var carts = await _context.Set<ShoppingCart>()
            .Where(c => c.Status == CartStatus.Abandoned && c.LastActivityAt >= since)
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);
            
        return carts.Sum(c => c.Total.Amount);
    }
}
