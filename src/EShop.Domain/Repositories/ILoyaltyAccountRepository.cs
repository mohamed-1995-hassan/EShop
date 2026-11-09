using EShop.Domain.Aggregates.LoyaltyAggregate;

namespace EShop.Domain.Repositories;

public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LoyaltyAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<LoyaltyAccount?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoyaltyAccount>> GetByTierAsync(LoyaltyTier tier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoyaltyAccount>> GetAccountsWithExpiringPointsAsync(int withinDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoyaltyAccount>> GetTopMembersAsync(int count, CancellationToken cancellationToken = default);
    Task AddAsync(LoyaltyAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(LoyaltyAccount account, CancellationToken cancellationToken = default);
    Task<int> GetTotalActiveAccountsAsync(CancellationToken cancellationToken = default);
    Task<long> GetTotalOutstandingPointsAsync(CancellationToken cancellationToken = default);
}
