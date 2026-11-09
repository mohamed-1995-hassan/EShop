using EShop.Application.Commands.Payments;
using EShop.Application.Queries.Payments;
using EShop.Domain.Aggregates.PaymentAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDetailsDto>> GetPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Get payment by order ID
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(PaymentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDetailsDto>> GetPaymentByOrder(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentByOrderQuery(orderId), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Initiate payment for an order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePaymentResult>> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePaymentCommand(
            request.OrderId,
            request.CustomerId,
            request.Amount,
            request.Currency,
            request.PaymentMethod,
            request.PaymentMethodDetails);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetPayment), new { id = result.PaymentId }, result);
    }

    /// <summary>
    /// Authorize payment
    /// </summary>
    [HttpPost("{id:guid}/authorize")]
    [ProducesResponseType(typeof(PaymentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentOperationResult>> AuthorizePayment(
        Guid id,
        [FromBody] AuthorizePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AuthorizePaymentCommand(id, request.GatewayTransactionId, request.GatewayResponse), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Capture authorized payment
    /// </summary>
    [HttpPost("{id:guid}/capture")]
    [ProducesResponseType(typeof(PaymentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentOperationResult>> CapturePayment(
        Guid id,
        [FromBody] CapturePaymentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new CapturePaymentCommand(id, request?.CaptureTransactionId), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Process full payment (authorize + capture)
    /// </summary>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(PaymentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentOperationResult>> ProcessPayment(
        Guid id,
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ProcessPaymentCommand(id, request.GatewayTransactionId, request.GatewayResponse), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Request refund
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(RefundResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RefundResult>> RequestRefund(
        Guid id,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RequestRefundCommand(id, request.Amount, request.Reason, request.RefundType), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Process a pending refund
    /// </summary>
    [HttpPost("{paymentId:guid}/refund/{refundId:guid}/process")]
    [ProducesResponseType(typeof(RefundResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RefundResult>> ProcessRefund(
        Guid paymentId,
        Guid refundId,
        [FromBody] ProcessRefundRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ProcessRefundCommand(paymentId, refundId, request.GatewayRefundId), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Void authorized payment
    /// </summary>
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(PaymentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentOperationResult>> VoidPayment(
        Guid id,
        [FromBody] VoidPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VoidPaymentCommand(id, request.Reason), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Get customer payments
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(List<PaymentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentSummaryDto>>> GetCustomerPayments(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerPaymentsQuery(customerId), cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record CreatePaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    PaymentMethodType PaymentMethod,
    string? PaymentMethodDetails = null);

public record AuthorizePaymentRequest(
    string GatewayTransactionId,
    string? GatewayResponse = null);

public record CapturePaymentRequest(string? CaptureTransactionId = null);

public record ProcessPaymentRequest(
    string GatewayTransactionId,
    string? GatewayResponse = null);

public record RefundRequest(
    decimal Amount,
    string Reason,
    RefundType RefundType = RefundType.CustomerRequest);

public record ProcessRefundRequest(string GatewayRefundId);

public record VoidPaymentRequest(string Reason);
