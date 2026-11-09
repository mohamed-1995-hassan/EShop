using EShop.Domain.Aggregates.WishlistAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly EShopDbContext _context;

    public WishlistRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Wishlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Wishlist>()
            .Include(w => w.Items)
            .Include(w => w.Collaborators)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Wishlist?> GetDefaultByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Wishlist>()
            .Include(w => w.Items)
            .Include(w => w.Collaborators)
            .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.IsDefault, cancellationToken);
    }

    public async Task<IReadOnlyList<Wishlist>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Wishlist>()
            .Include(w => w.Items)
            .Where(w => w.CustomerId == customerId)
            .OrderByDescending(w => w.IsDefault)
            .ThenByDescending(w => w.LastModifiedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Wishlist?> GetByShareTokenAsync(string shareToken, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Wishlist>()
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.ShareToken == shareToken, cancellationToken);
    }

    public async Task<bool> IsProductInWishlistAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Wishlist>()
            .Where(w => w.CustomerId == customerId)
            .SelectMany(w => w.Items)
            .AnyAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<Wishlist>> GetPublicWishlistsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Wishlist>()
            .Include(w => w.Items)
            .Where(w => w.Privacy == WishlistPrivacy.Public)
            .OrderByDescending(w => w.LastModifiedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
    {
        await _context.Set<Wishlist>().AddAsync(wishlist, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Wishlist wishlist, CancellationToken cancellationToken = default)
    {
        _context.Set<Wishlist>().Update(wishlist);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wishlist = await GetByIdAsync(id, cancellationToken);
        if (wishlist != null)
        {
            _context.Set<Wishlist>().Remove(wishlist);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
