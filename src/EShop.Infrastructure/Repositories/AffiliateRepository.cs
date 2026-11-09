using EShop.Domain.Aggregates.AffiliateAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

public class AffiliateRepository : IAffiliateRepository
{
    private static readonly ConcurrentDictionary<Guid, Affiliate> _affiliates = new();

    public Task<Affiliate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_affiliates.GetValueOrDefault(id));

    public Task<Affiliate?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_affiliates.Values.FirstOrDefault(a => a.UserId == userId));

    public Task<Affiliate?> GetByCodeAsync(string affiliateCode, CancellationToken cancellationToken = default)
        => Task.FromResult(_affiliates.Values.FirstOrDefault(a => a.AffiliateCode == affiliateCode));

    public Task<IReadOnlyList<Affiliate>> GetByStatusAsync(AffiliateStatus status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Affiliate>>(_affiliates.Values.Where(a => a.Status == status).ToList());

    public Task<IReadOnlyList<Affiliate>> GetByTierAsync(AffiliateTier tier, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Affiliate>>(_affiliates.Values.Where(a => a.Tier == tier).ToList());

    public Task<IReadOnlyList<Affiliate>> GetTopPerformersAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Affiliate>>(_affiliates.Values
            .Where(a => a.Status == AffiliateStatus.Active)
            .OrderByDescending(a => a.TotalEarnings.Amount)
            .Take(count)
            .ToList());

    public Task<IReadOnlyList<Affiliate>> GetPendingPayoutsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Affiliate>>(_affiliates.Values
            .Where(a => a.Status == AffiliateStatus.Active && 
                        a.PendingEarnings.Amount >= a.MinimumPayoutThreshold.Amount)
            .ToList());

    public Task AddAsync(Affiliate affiliate, CancellationToken cancellationToken = default)
    {
        _affiliates[affiliate.Id] = affiliate;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Affiliate affiliate, CancellationToken cancellationToken = default)
    {
        _affiliates[affiliate.Id] = affiliate;
        return Task.CompletedTask;
    }
}
