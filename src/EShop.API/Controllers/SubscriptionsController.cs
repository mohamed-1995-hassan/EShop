using EShop.Application.Commands.Subscriptions;
using EShop.Application.Queries.Subscriptions;
using EShop.Domain.Aggregates.SubscriptionAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionDto>> Get(Guid id)
    {
        var subscription = await _mediator.Send(new GetSubscriptionQuery(id));
        return subscription != null ? Ok(subscription) : NotFound();
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionDto>>> GetByCustomer(Guid customerId)
    {
        var subscriptions = await _mediator.Send(new GetCustomerSubscriptionsQuery(customerId));
        return Ok(subscriptions);
    }

    [HttpGet("due-for-billing")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionDto>>> GetDueForBilling([FromQuery] DateTime? date = null)
    {
        var subscriptions = await _mediator.Send(new GetSubscriptionsDueForBillingQuery(date ?? DateTime.UtcNow));
        return Ok(subscriptions);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSubscriptionRequest request)
    {
        var id = await _mediator.Send(new CreateSubscriptionCommand(
            request.CustomerId,
            request.Type,
            request.BillingCycle,
            request.Street,
            request.City,
            request.Country,
            request.PostalCode,
            request.DiscountPercentage,
            request.BillingDayOfMonth));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{subscriptionId:guid}/items")]
    public async Task<ActionResult<Guid>> AddItem(Guid subscriptionId, [FromBody] AddSubscriptionItemRequest request)
    {
        var id = await _mediator.Send(new AddSubscriptionItemCommand(
            subscriptionId,
            request.ProductId,
            request.ProductName,
            request.UnitPrice,
            request.Currency,
            request.Quantity,
            request.Variant));
        return Ok(id);
    }

    [HttpPost("{subscriptionId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid subscriptionId, [FromBody] ActivateSubscriptionRequest request)
    {
        await _mediator.Send(new ActivateSubscriptionCommand(subscriptionId, request.PaymentMethodId));
        return NoContent();
    }

    [HttpPost("{subscriptionId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid subscriptionId, [FromBody] PauseSubscriptionRequest? request = null)
    {
        await _mediator.Send(new PauseSubscriptionCommand(subscriptionId, request?.Until, request?.Reason));
        return NoContent();
    }

    [HttpPost("{subscriptionId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid subscriptionId)
    {
        await _mediator.Send(new ResumeSubscriptionCommand(subscriptionId));
        return NoContent();
    }

    [HttpPost("{subscriptionId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid subscriptionId, [FromBody] CancelSubscriptionRequest request)
    {
        await _mediator.Send(new CancelSubscriptionCommand(subscriptionId, request.Reason, request.Immediate));
        return NoContent();
    }

    [HttpPost("{subscriptionId:guid}/skip-delivery")]
    public async Task<IActionResult> SkipDelivery(Guid subscriptionId, [FromBody] SkipDeliveryRequest? request = null)
    {
        await _mediator.Send(new SkipNextDeliveryCommand(subscriptionId, request?.Reason));
        return NoContent();
    }
}

// Request DTOs
public record CreateSubscriptionRequest(
    Guid CustomerId,
    SubscriptionType Type,
    BillingCycle BillingCycle,
    string Street,
    string City,
    string Country,
    string PostalCode,
    decimal DiscountPercentage = 10,
    int BillingDayOfMonth = 1);

public record AddSubscriptionItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    string? Variant = null);

public record ActivateSubscriptionRequest(Guid PaymentMethodId);
public record PauseSubscriptionRequest(DateTime? Until = null, string? Reason = null);
public record CancelSubscriptionRequest(string Reason, bool Immediate = false);
public record SkipDeliveryRequest(string? Reason = null);
