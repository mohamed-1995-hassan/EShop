using EShop.Application.Commands.Wishlists;
using EShop.Application.Queries.Wishlists;
using EShop.Domain.Aggregates.WishlistAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WishlistsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WishlistDto>> Get(Guid id)
    {
        var wishlist = await _mediator.Send(new GetWishlistQuery(id));
        return wishlist != null ? Ok(wishlist) : NotFound();
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<WishlistDto>>> GetByCustomer(Guid customerId)
    {
        var wishlists = await _mediator.Send(new GetCustomerWishlistsQuery(customerId));
        return Ok(wishlists);
    }

    [HttpGet("shared/{shareToken}")]
    public async Task<ActionResult<WishlistDto>> GetByShareToken(string shareToken)
    {
        var wishlist = await _mediator.Send(new GetSharedWishlistQuery(shareToken));
        return wishlist != null ? Ok(wishlist) : NotFound();
    }

    [HttpGet("customer/{customerId:guid}/product/{productId:guid}/exists")]
    public async Task<ActionResult<bool>> CheckProductInWishlist(Guid customerId, Guid productId)
    {
        var exists = await _mediator.Send(new IsProductInWishlistQuery(customerId, productId));
        return Ok(exists);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateWishlistRequest request)
    {
        var id = await _mediator.Send(new CreateWishlistCommand(
            request.CustomerId,
            request.Name,
            request.Type,
            request.Privacy,
            request.Description));
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("default")]
    public async Task<ActionResult<Guid>> CreateDefault([FromBody] CreateDefaultWishlistRequest request)
    {
        var id = await _mediator.Send(new CreateDefaultWishlistCommand(request.CustomerId));
        return Ok(id);
    }

    [HttpPost("{wishlistId:guid}/items")]
    public async Task<ActionResult<Guid>> AddItem(Guid wishlistId, [FromBody] AddWishlistItemRequest request)
    {
        var id = await _mediator.Send(new AddWishlistItemCommand(
            wishlistId,
            request.ProductId,
            request.ProductName,
            request.Price,
            request.Currency,
            request.ImageUrl,
            request.DesiredQuantity,
            request.Notes));
        return Ok(id);
    }

    [HttpDelete("{wishlistId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid wishlistId, Guid itemId)
    {
        await _mediator.Send(new RemoveWishlistItemCommand(wishlistId, itemId));
        return NoContent();
    }

    [HttpPost("{wishlistId:guid}/items/{itemId:guid}/purchased")]
    public async Task<IActionResult> MarkItemPurchased(Guid wishlistId, Guid itemId, [FromBody] MarkPurchasedRequest request)
    {
        await _mediator.Send(new MarkWishlistItemPurchasedCommand(wishlistId, itemId, request.PurchasedBy));
        return NoContent();
    }

    [HttpPut("{wishlistId:guid}/privacy")]
    public async Task<IActionResult> UpdatePrivacy(Guid wishlistId, [FromBody] UpdatePrivacyRequest request)
    {
        await _mediator.Send(new UpdateWishlistPrivacyCommand(wishlistId, request.Privacy));
        return NoContent();
    }
}

// Request DTOs
public record CreateWishlistRequest(
    Guid CustomerId,
    string Name,
    WishlistType Type,
    WishlistPrivacy Privacy = WishlistPrivacy.Private,
    string? Description = null);

public record CreateDefaultWishlistRequest(Guid CustomerId);

public record AddWishlistItemRequest(
    Guid ProductId,
    string ProductName,
    decimal Price,
    string Currency,
    string? ImageUrl = null,
    int DesiredQuantity = 1,
    string? Notes = null);

public record MarkPurchasedRequest(Guid? PurchasedBy = null);
public record UpdatePrivacyRequest(WishlistPrivacy Privacy);
