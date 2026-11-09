using EShop.Domain.Aggregates.PaymentAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Payments;

// ============================================
// CREATE PAYMENT
// ============================================
public record CreatePaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    PaymentMethodType PaymentMethod,
    string? PaymentMethodDetails = null) : IRequest<CreatePaymentResult>;

public record CreatePaymentResult(
    bool Success,
    Guid? PaymentId,
    string? ErrorMessage);

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public CreatePaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<CreatePaymentResult> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var amount = new Money(request.Amount, request.Currency);
            var payment = Payment.Create(
                request.OrderId,
                request.CustomerId,
                amount,
                request.PaymentMethod,
                request.PaymentMethodDetails);

            await _paymentRepository.AddAsync(payment, cancellationToken);

            return new CreatePaymentResult(true, payment.Id, null);
        }
        catch (Exception ex)
        {
            return new CreatePaymentResult(false, null, ex.Message);
        }
    }
}

// ============================================
// AUTHORIZE PAYMENT
// ============================================
public record AuthorizePaymentCommand(
    Guid PaymentId,
    string GatewayTransactionId,
    string? GatewayResponse = null) : IRequest<PaymentOperationResult>;

public record PaymentOperationResult(
    bool Success,
    string? Status,
    string? ErrorMessage);

public class AuthorizePaymentCommandHandler : IRequestHandler<AuthorizePaymentCommand, PaymentOperationResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public AuthorizePaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentOperationResult> Handle(AuthorizePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
                return new PaymentOperationResult(false, null, "Payment not found");

            payment.Authorize(request.GatewayTransactionId, request.GatewayResponse);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return new PaymentOperationResult(true, payment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new PaymentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// CAPTURE PAYMENT
// ============================================
public record CapturePaymentCommand(
    Guid PaymentId,
    string? CaptureTransactionId = null) : IRequest<PaymentOperationResult>;

public class CapturePaymentCommandHandler : IRequestHandler<CapturePaymentCommand, PaymentOperationResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public CapturePaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentOperationResult> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
                return new PaymentOperationResult(false, null, "Payment not found");

            payment.Capture(request.CaptureTransactionId);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return new PaymentOperationResult(true, payment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new PaymentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// PROCESS PAYMENT (AUTHORIZE + CAPTURE)
// ============================================
public record ProcessPaymentCommand(
    Guid PaymentId,
    string GatewayTransactionId,
    string? GatewayResponse = null) : IRequest<PaymentOperationResult>;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentOperationResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public ProcessPaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentOperationResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
                return new PaymentOperationResult(false, null, "Payment not found");

            payment.AuthorizeAndCapture(request.GatewayTransactionId, request.GatewayResponse);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return new PaymentOperationResult(true, payment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new PaymentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// VOID PAYMENT
// ============================================
public record VoidPaymentCommand(Guid PaymentId, string Reason) : IRequest<PaymentOperationResult>;

public class VoidPaymentCommandHandler : IRequestHandler<VoidPaymentCommand, PaymentOperationResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public VoidPaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentOperationResult> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
                return new PaymentOperationResult(false, null, "Payment not found");

            payment.Void(request.Reason);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return new PaymentOperationResult(true, payment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new PaymentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// REQUEST REFUND
// ============================================
public record RequestRefundCommand(
    Guid PaymentId,
    decimal Amount,
    string Reason,
    RefundType RefundType = RefundType.CustomerRequest) : IRequest<RefundResult>;

public record RefundResult(
    bool Success,
    Guid? RefundId,
    decimal? RefundAmount,
    string? ErrorMessage);

public class RequestRefundCommandHandler : IRequestHandler<RequestRefundCommand, RefundResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public RequestRefundCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<RefundResult> Handle(RequestRefundCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
                return new RefundResult(false, null, null, "Payment not found");

            var refundAmount = new Money(request.Amount, payment.Amount.Currency);
            var refund = payment.RequestRefund(refundAmount, request.Reason, request.RefundType);
            
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            return new RefundResult(true, refund.Id, refund.Amount.Amount, null);
        }
        catch (Exception ex)
        {
            return new RefundResult(false, null, null, ex.Message);
        }
    }
}

// ============================================
// PROCESS REFUND
// ============================================
public record ProcessRefundCommand(
    Guid PaymentId,
    Guid RefundId,
    string GatewayRefundId) : IRequest<RefundResult>;

public class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, RefundResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public ProcessRefundCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<RefundResult> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
                return new RefundResult(false, null, null, "Payment not found");

            payment.ProcessRefund(request.RefundId, request.GatewayRefundId);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            var refund = payment.Refunds.First(r => r.Id == request.RefundId);
            return new RefundResult(true, refund.Id, refund.Amount.Amount, null);
        }
        catch (Exception ex)
        {
            return new RefundResult(false, null, null, ex.Message);
        }
    }
}
