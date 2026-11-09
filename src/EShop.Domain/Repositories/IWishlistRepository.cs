using EShop.Domain.Aggregates.WishlistAggregate;

namespace EShop.Domain.Repositories;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Wishlist?> GetByShareTokenAsync(string shareToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wishlist>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Wishlist?> GetDefaultByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wishlist>> GetPublicWishlistsAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<bool> IsProductInWishlistAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    
    Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken = default);
    Task UpdateAsync(Wishlist wishlist, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
