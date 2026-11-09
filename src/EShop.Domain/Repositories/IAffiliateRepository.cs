using EShop.Domain.Aggregates.AffiliateAggregate;

namespace EShop.Domain.Repositories;

public interface IAffiliateRepository
{
    Task<Affiliate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Affiliate?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Affiliate?> GetByCodeAsync(string affiliateCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Affiliate>> GetByStatusAsync(AffiliateStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Affiliate>> GetByTierAsync(AffiliateTier tier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Affiliate>> GetTopPerformersAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Affiliate>> GetPendingPayoutsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Affiliate affiliate, CancellationToken cancellationToken = default);
    Task UpdateAsync(Affiliate affiliate, CancellationToken cancellationToken = default);
}
