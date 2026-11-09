using EShop.Application.Commands.Cart;
using EShop.Application.Queries.Cart;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current cart
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> GetCart(
        [FromQuery] Guid? customerId,
        [FromQuery] string sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCartQuery(customerId, sessionId), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartOperationResult>> AddItem(
        [FromBody] AddToCartRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddToCartCommand(
            request.CustomerId,
            request.SessionId,
            request.ProductId,
            request.ProductName,
            request.UnitPrice,
            request.Quantity,
            request.ImageUrl,
            request.Attributes);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("{cartId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartOperationResult>> UpdateItemQuantity(
        Guid cartId,
        Guid itemId,
        [FromBody] UpdateQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCartItemCommand(cartId, itemId, request.Quantity), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("{cartId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartOperationResult>> RemoveItem(
        Guid cartId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveCartItemCommand(cartId, itemId), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Apply promo code
    /// </summary>
    [HttpPost("{cartId:guid}/promo")]
    [ProducesResponseType(typeof(ApplyPromoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplyPromoResult>> ApplyPromoCode(
        Guid cartId,
        [FromBody] ApplyPromoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ApplyPromoCodeCommand(cartId, request.PromoCode), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Start checkout process
    /// </summary>
    [HttpPost("{cartId:guid}/checkout")]
    [ProducesResponseType(typeof(CheckoutResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CheckoutResult>> StartCheckout(
        Guid cartId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartCheckoutCommand(cartId), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Get abandoned carts (Admin)
    /// </summary>
    [HttpGet("abandoned")]
    [ProducesResponseType(typeof(List<AbandonedCartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AbandonedCartDto>>> GetAbandonedCarts(
        [FromQuery] int hoursAbandoned = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAbandonedCartsQuery(hoursAbandoned), cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record AddToCartRequest(
    Guid? CustomerId,
    string SessionId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? ImageUrl = null,
    Dictionary<string, string>? Attributes = null);

public record UpdateQuantityRequest(int Quantity);
public record ApplyPromoRequest(string PromoCode);
