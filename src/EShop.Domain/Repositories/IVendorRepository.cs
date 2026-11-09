using EShop.Domain.Aggregates.VendorAggregate;

namespace EShop.Domain.Repositories;

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vendor?> GetByCodeAsync(string vendorCode, CancellationToken cancellationToken = default);
    Task<Vendor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetByStatusAsync(VendorStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetByTierAsync(VendorTier tier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetActiveVendorsAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetTopVendorsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetVendorsDueForPayoutAsync(PayoutSchedule schedule, CancellationToken cancellationToken = default);
    
    Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default);
    
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    
    // Analytics
    Task<int> GetActiveVendorCountAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalVendorRevenueAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
