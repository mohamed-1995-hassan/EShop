using EShop.Domain.Aggregates.VendorAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly EShopDbContext _context;

    public VendorRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Include(v => v.Categories)
            .Include(v => v.Documents)
            .Include(v => v.Payouts)
            .Include(v => v.Notes)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<Vendor?> GetByCodeAsync(string vendorCode, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Include(v => v.Categories)
            .FirstOrDefaultAsync(v => v.VendorCode == vendorCode, cancellationToken);
    }

    public async Task<Vendor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .FirstOrDefaultAsync(v => v.ContactEmail == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetByStatusAsync(VendorStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Where(v => v.Status == status)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetActiveVendorsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Include(v => v.Categories.Where(c => c.IsApproved))
            .Where(v => v.Status == VendorStatus.Active)
            .OrderByDescending(v => v.TotalRevenue.Amount)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetPendingApprovalAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Include(v => v.Documents)
            .Where(v => v.Status == VendorStatus.UnderReview)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetByTierAsync(VendorTier tier, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Where(v => v.Tier == tier && v.Status == VendorStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Where(v => v.Status == VendorStatus.Active &&
                        v.Categories.Any(c => c.CategoryId == categoryId && c.IsApproved))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetTopVendorsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Where(v => v.Status == VendorStatus.Active)
            .OrderByDescending(v => v.TotalRevenue.Amount)
            .ThenByDescending(v => v.AverageRating)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetVendorsDueForPayoutAsync(PayoutSchedule schedule, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .Where(v => v.Status == VendorStatus.Active && 
                        v.PayoutSchedule == schedule &&
                        v.BankAccountNumber != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .AnyAsync(v => v.ContactEmail == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<int> GetActiveVendorCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Vendor>()
            .CountAsync(v => v.Status == VendorStatus.Active, cancellationToken);
    }

    public async Task<decimal> GetTotalVendorRevenueAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // This would need a more complex implementation with order/payout tracking
        // For now, return sum of total revenue from active vendors
        return await _context.Set<Vendor>()
            .Where(v => v.Status == VendorStatus.Active)
            .SumAsync(v => v.TotalRevenue.Amount, cancellationToken);
    }

    public async Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        await _context.Set<Vendor>().AddAsync(vendor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        _context.Set<Vendor>().Update(vendor);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
