using EShop.Application.Commands.Orders;
using EShop.Application.Queries.Orders;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderAddressDto = EShop.Application.Commands.Orders.AddressDto;
using OrderItemDto = EShop.Application.Commands.Orders.OrderItemDto;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailsDto>> GetOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Get orders for a customer
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetCustomerOrders(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCustomerOrdersQuery(customerId, page, pageSize), 
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateOrderResult>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.CustomerId,
            new OrderAddressDto(
                request.ShippingAddress.Street,
                request.ShippingAddress.City,
                request.ShippingAddress.Country,
                request.ShippingAddress.PostalCode),
            request.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity)).ToList(),
            request.PromoCode,
            request.LoyaltyPointsToRedeem);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(CancelOrderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CancelOrderResult>> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelOrderCommand(id, request.Reason), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Ship an order
    /// </summary>
    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(ShipOrderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipOrderResult>> ShipOrder(
        Guid id,
        [FromBody] ShipOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ShipOrderCommand(id, request.TrackingNumber, request.CarrierName), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Confirm order delivery
    /// </summary>
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeliverOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeliverOrderCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Add item to order (before shipping)
    /// </summary>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(ModifyOrderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ModifyOrderResult>> AddOrderItem(
        Guid id,
        [FromBody] AddOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddOrderItemCommand(id, request.ProductId, request.ProductName, request.UnitPrice, request.Quantity), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }
}

// Request DTOs
public record CreateOrderRequest(
    Guid CustomerId,
    AddressRequest ShippingAddress,
    List<OrderItemRequest> Items,
    string? PromoCode = null,
    int? LoyaltyPointsToRedeem = null);

public record AddressRequest(
    string Street,
    string City,
    string Country,
    string PostalCode);

public record OrderItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

public record CancelOrderRequest(string Reason);
public record ShipOrderRequest(string TrackingNumber, string CarrierName);
public record AddOrderItemRequest(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
