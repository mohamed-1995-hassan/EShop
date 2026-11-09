using EShop.Domain.Aggregates.PaymentAggregate;

namespace EShop.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByGatewayTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetPendingPaymentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetFailedPaymentsForRetryAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    
    // Reporting
    Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRefundsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
