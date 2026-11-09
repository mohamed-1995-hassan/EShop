using EShop.Domain.Aggregates.CustomerAggregate;
using EShop.Domain.Specifications;

namespace EShop.Domain.Repositories;

public interface ICustomerProfileRepository
{
    Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetWithAddressesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetWithWalletAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetWithLoyaltyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerProfile?> GetFullProfileAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<CustomerProfile>> GetByTierAsync(CustomerTier tier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerProfile>> GetTopCustomersAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerProfile>> GetInactiveCustomersAsync(int daysSinceLastOrder, CancellationToken cancellationToken = default);
    
    Task AddAsync(CustomerProfile customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomerProfile customer, CancellationToken cancellationToken = default);
    
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<int> GetReferralCountAsync(Guid customerId, CancellationToken cancellationToken = default);
}
