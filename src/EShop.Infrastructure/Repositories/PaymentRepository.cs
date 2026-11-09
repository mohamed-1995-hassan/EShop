using EShop.Domain.Aggregates.PaymentAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly EShopDbContext _context;

    public PaymentRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task<Payment?> GetByGatewayTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.GatewayTransactionId == transactionId, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.Status == PaymentStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetFailedPaymentsForRetryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.Status == PaymentStatus.Failed && p.AttemptCount < 3)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Set<Payment>().AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Set<Payment>().Update(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.Status == PaymentStatus.Captured && 
                        p.ProcessedAt >= startDate && 
                        p.ProcessedAt <= endDate)
            .SumAsync(p => p.Amount.Amount, cancellationToken);
    }

    public async Task<decimal> GetTotalRefundsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .SelectMany(p => p.Refunds)
            .Where(r => r.Status == RefundStatus.Completed &&
                        r.ProcessedAt >= startDate &&
                        r.ProcessedAt <= endDate)
            .SumAsync(r => r.Amount.Amount, cancellationToken);
    }
}
