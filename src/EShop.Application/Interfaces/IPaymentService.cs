namespace EShop.Application.Interfaces;

/// <summary>
/// Payment service interface for payment processing.
/// </summary>
public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(
        Guid orderId,
        Guid customerId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
    
    Task RefundAsync(
        Guid orderId,
        Guid customerId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default);
}
