using EShop.Domain.Aggregates.ReturnAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class ReturnRepository : IReturnRepository
{
    private readonly EShopDbContext _context;

    public ReturnRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Include(r => r.Items)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ReturnRequest?> GetByReturnNumberAsync(string returnNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ReturnNumber == returnNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.OrderId == orderId)
            .Include(r => r.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetByStatusAsync(ReturnStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.Status == ReturnStatus.PendingApproval)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetAwaitingInspectionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.Status == ReturnStatus.Received || r.Status == ReturnStatus.Inspecting)
            .Include(r => r.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetInTransitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.Status == ReturnStatus.InTransit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
    {
        await _context.Set<ReturnRequest>().AddAsync(returnRequest, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default)
    {
        _context.Set<ReturnRequest>().Update(returnRequest);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetReturnCountByReasonAsync(ReturnReason reason, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .SelectMany(r => r.Items)
            .CountAsync(i => i.Reason == reason, cancellationToken);
    }

    public async Task<decimal> GetTotalRefundsIssuedAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReturnRequest>()
            .Where(r => r.Status == ReturnStatus.Completed &&
                        r.CompletedAt >= startDate &&
                        r.CompletedAt <= endDate)
            .SumAsync(r => r.RefundAmount.Amount, cancellationToken);
    }
}
