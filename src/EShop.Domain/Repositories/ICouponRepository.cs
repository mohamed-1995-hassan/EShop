using EShop.Domain.Aggregates.CouponAggregate;

namespace EShop.Domain.Repositories;

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Coupon>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Coupon>> GetByStatusAsync(CouponStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Coupon>> GetExpiringSoonAsync(int withinDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Coupon>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<int> GetUsageCountByCustomerAsync(Guid couponId, Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Coupon> coupons, CancellationToken cancellationToken = default);
    Task UpdateAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
