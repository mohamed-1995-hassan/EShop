using EShop.Application.Commands.GiftCards;
using EShop.Application.Queries.GiftCards;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GiftCardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GiftCardsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GiftCardDto>> Get(Guid id)
    {
        var giftCard = await _mediator.Send(new GetGiftCardQuery(id));
        return giftCard != null ? Ok(giftCard) : NotFound();
    }

    [HttpGet("balance/{code}")]
    public async Task<ActionResult<GiftCardBalanceDto>> CheckBalance(string code)
    {
        var balance = await _mediator.Send(new CheckGiftCardBalanceQuery(code));
        return balance != null ? Ok(balance) : NotFound();
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<GiftCardDto>>> GetByCustomer(Guid customerId)
    {
        var giftCards = await _mediator.Send(new GetCustomerGiftCardsQuery(customerId));
        return Ok(giftCards);
    }

    [HttpGet("liability")]
    public async Task<ActionResult<decimal>> GetTotalLiability()
    {
        var liability = await _mediator.Send(new GetGiftCardLiabilityQuery());
        return Ok(liability);
    }

    [HttpPost("digital")]
    public async Task<ActionResult<Guid>> CreateDigital([FromBody] CreateDigitalGiftCardRequest request)
    {
        var id = await _mediator.Send(new CreateDigitalGiftCardCommand(
            request.Amount,
            request.Currency,
            request.PurchasedByCustomerId,
            request.RecipientEmail,
            request.RecipientName,
            request.PersonalMessage,
            request.ScheduledDelivery,
            request.ExpiryYears));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("physical")]
    public async Task<ActionResult<Guid>> CreatePhysical([FromBody] CreatePhysicalGiftCardRequest request)
    {
        var id = await _mediator.Send(new CreatePhysicalGiftCardCommand(
            request.Amount,
            request.Currency,
            request.PurchasedByCustomerId,
            request.ExpiryYears));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("store-credit")]
    public async Task<ActionResult<Guid>> CreateStoreCredit([FromBody] CreateStoreCreditRequest request)
    {
        var id = await _mediator.Send(new CreateStoreCreditCommand(
            request.Amount,
            request.Currency,
            request.CustomerId,
            request.Reason,
            request.IsReloadable));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _mediator.Send(new ActivateGiftCardCommand(id));
        return NoContent();
    }

    [HttpPost("redeem")]
    public async Task<ActionResult<decimal>> Redeem([FromBody] RedeemGiftCardRequest request)
    {
        var redeemedAmount = await _mediator.Send(new RedeemGiftCardCommand(
            request.Code,
            request.Amount,
            request.Currency,
            request.OrderId,
            request.OrderNumber));
        return Ok(redeemedAmount);
    }

    [HttpPost("{id:guid}/reload")]
    public async Task<IActionResult> Reload(Guid id, [FromBody] ReloadGiftCardRequest request)
    {
        await _mediator.Send(new ReloadGiftCardCommand(id, request.Amount, request.Currency));
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendGiftCardRequest request)
    {
        await _mediator.Send(new SuspendGiftCardCommand(id, request.Reason));
        return NoContent();
    }
}

// Request DTOs
public record CreateDigitalGiftCardRequest(
    decimal Amount,
    string Currency,
    Guid PurchasedByCustomerId,
    string RecipientEmail,
    string RecipientName,
    string? PersonalMessage = null,
    DateTime? ScheduledDelivery = null,
    int ExpiryYears = 2);

public record CreatePhysicalGiftCardRequest(
    decimal Amount,
    string Currency,
    Guid? PurchasedByCustomerId = null,
    int ExpiryYears = 3);

public record CreateStoreCreditRequest(
    decimal Amount,
    string Currency,
    Guid CustomerId,
    string Reason,
    bool IsReloadable = true);

public record RedeemGiftCardRequest(
    string Code,
    decimal Amount,
    string Currency,
    Guid OrderId,
    string OrderNumber);

public record ReloadGiftCardRequest(decimal Amount, string Currency);
public record SuspendGiftCardRequest(string Reason);
