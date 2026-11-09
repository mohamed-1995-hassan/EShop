using EShop.Domain.Aggregates.CouponAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

public class CouponRepository : ICouponRepository
{
    private static readonly ConcurrentDictionary<Guid, Coupon> _coupons = new();

    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_coupons.GetValueOrDefault(id));

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_coupons.Values.FirstOrDefault(c => c.Code == code.ToUpperInvariant()));

    public Task<IReadOnlyList<Coupon>> GetActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Coupon>>(_coupons.Values.Where(c => c.IsActive).ToList());

    public Task<IReadOnlyList<Coupon>> GetByStatusAsync(CouponStatus status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Coupon>>(_coupons.Values.Where(c => c.Status == status).ToList());

    public Task<IReadOnlyList<Coupon>> GetExpiringSoonAsync(int withinDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        return Task.FromResult<IReadOnlyList<Coupon>>(_coupons.Values
            .Where(c => c.ValidUntil.HasValue && c.ValidUntil.Value <= cutoff && c.IsActive)
            .ToList());
    }

    public Task<IReadOnlyList<Coupon>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Coupon>>(_coupons.Values.Where(c => c.CampaignId == campaignId).ToList());

    public Task<int> GetUsageCountByCustomerAsync(Guid couponId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var coupon = _coupons.GetValueOrDefault(couponId);
        return Task.FromResult(coupon?.Usages.Count(u => u.CustomerId == customerId && !u.IsReversed) ?? 0);
    }

    public Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        _coupons[coupon.Id] = coupon;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Coupon> coupons, CancellationToken cancellationToken = default)
    {
        foreach (var coupon in coupons)
            _coupons[coupon.Id] = coupon;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        _coupons[coupon.Id] = coupon;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _coupons.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
