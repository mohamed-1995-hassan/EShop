using EShop.Domain.Aggregates.PaymentAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Payments;

// ============================================
// GET PAYMENT BY ID
// ============================================
public record GetPaymentQuery(Guid PaymentId) : IRequest<PaymentDetailsDto?>;

public record PaymentDetailsDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    decimal RefundedAmount,
    string Status,
    string PaymentMethod,
    string? PaymentMethodDetails,
    string? GatewayTransactionId,
    int AttemptCount,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    List<PaymentTransactionDto> Transactions,
    List<RefundDto> Refunds);

public record PaymentTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string GatewayTransactionId,
    string? Description,
    DateTime CreatedAt);

public record RefundDto(
    Guid Id,
    decimal Amount,
    string Reason,
    string Type,
    string Status,
    string? GatewayRefundId,
    DateTime RequestedAt,
    DateTime? ProcessedAt);

public class GetPaymentQueryHandler : IRequestHandler<GetPaymentQuery, PaymentDetailsDto?>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentDetailsDto?> Handle(GetPaymentQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment == null) return null;

        return MapToDto(payment);
    }

    private static PaymentDetailsDto MapToDto(Payment payment)
    {
        return new PaymentDetailsDto(
            payment.Id,
            payment.OrderId,
            payment.CustomerId,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.RefundedAmount.Amount,
            payment.Status.ToString(),
            payment.PaymentMethod.ToString(),
            payment.PaymentMethodDetails,
            payment.GatewayTransactionId,
            payment.AttemptCount,
            payment.CreatedAt,
            payment.ProcessedAt,
            payment.Transactions.Select(t => new PaymentTransactionDto(
                t.Id,
                t.Type.ToString(),
                t.Amount.Amount,
                t.GatewayTransactionId,
                t.Description,
                t.CreatedAt)).ToList(),
            payment.Refunds.Select(r => new RefundDto(
                r.Id,
                r.Amount.Amount,
                r.Reason,
                r.Type.ToString(),
                r.Status.ToString(),
                r.GatewayRefundId,
                r.RequestedAt,
                r.ProcessedAt)).ToList());
    }
}

// ============================================
// GET PAYMENT BY ORDER
// ============================================
public record GetPaymentByOrderQuery(Guid OrderId) : IRequest<PaymentDetailsDto?>;

public class GetPaymentByOrderQueryHandler : IRequestHandler<GetPaymentByOrderQuery, PaymentDetailsDto?>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentByOrderQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentDetailsDto?> Handle(GetPaymentByOrderQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (payment == null) return null;

        return new PaymentDetailsDto(
            payment.Id,
            payment.OrderId,
            payment.CustomerId,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.RefundedAmount.Amount,
            payment.Status.ToString(),
            payment.PaymentMethod.ToString(),
            payment.PaymentMethodDetails,
            payment.GatewayTransactionId,
            payment.AttemptCount,
            payment.CreatedAt,
            payment.ProcessedAt,
            payment.Transactions.Select(t => new PaymentTransactionDto(
                t.Id,
                t.Type.ToString(),
                t.Amount.Amount,
                t.GatewayTransactionId,
                t.Description,
                t.CreatedAt)).ToList(),
            payment.Refunds.Select(r => new RefundDto(
                r.Id,
                r.Amount.Amount,
                r.Reason,
                r.Type.ToString(),
                r.Status.ToString(),
                r.GatewayRefundId,
                r.RequestedAt,
                r.ProcessedAt)).ToList());
    }
}

// ============================================
// GET CUSTOMER PAYMENTS
// ============================================
public record GetCustomerPaymentsQuery(Guid CustomerId) : IRequest<List<PaymentSummaryDto>>;

public record PaymentSummaryDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    DateTime CreatedAt);

public class GetCustomerPaymentsQueryHandler : IRequestHandler<GetCustomerPaymentsQuery, List<PaymentSummaryDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetCustomerPaymentsQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<List<PaymentSummaryDto>> Handle(GetCustomerPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return payments.Select(p => new PaymentSummaryDto(
            p.Id,
            p.OrderId,
            p.Amount.Amount,
            p.Amount.Currency,
            p.Status.ToString(),
            p.PaymentMethod.ToString(),
            p.CreatedAt)).ToList();
    }
}
