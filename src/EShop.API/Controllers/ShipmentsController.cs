using EShop.Application.Commands.Shipments;
using EShop.Application.Queries.Shipments;
using EShop.Domain.Aggregates.ShippingAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShipmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShipmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get shipment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShipmentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentDetailsDto>> GetShipment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShipmentQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Track shipment by tracking number
    /// </summary>
    [HttpGet("track/{trackingNumber}")]
    [ProducesResponseType(typeof(TrackingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackingResultDto>> TrackShipment(
        string trackingNumber,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new TrackShipmentQuery(trackingNumber), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create shipment for order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateShipmentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateShipmentResult>> CreateShipment(
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateShipmentCommand(
            request.OrderId,
            request.CarrierId,
            request.CarrierName,
            new CreateShipmentAddressDto(
                request.ShippingAddress.Street,
                request.ShippingAddress.City,
                request.ShippingAddress.Country,
                request.ShippingAddress.PostalCode),
            request.ShipmentType,
            request.DeliveryInstructions,
            request.Items?.Select(i => new CreateShipmentItemDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.Weight)).ToList());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetShipment), new { id = result.ShipmentId }, result);
    }

    /// <summary>
    /// Assign tracking number
    /// </summary>
    [HttpPost("{id:guid}/tracking")]
    [ProducesResponseType(typeof(ShipmentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipmentOperationResult>> AssignTrackingNumber(
        Guid id,
        [FromBody] AssignTrackingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignTrackingNumberCommand(id, request.TrackingNumber), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Mark shipment as ready for pickup
    /// </summary>
    [HttpPost("{id:guid}/ready-for-pickup")]
    [ProducesResponseType(typeof(ShipmentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipmentOperationResult>> MarkReadyForPickup(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkReadyForPickupCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Mark shipment as picked up by carrier
    /// </summary>
    [HttpPost("{id:guid}/pickup")]
    [ProducesResponseType(typeof(ShipmentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipmentOperationResult>> MarkPickedUp(
        Guid id,
        [FromBody] PickupRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new MarkPickedUpCommand(id, request?.DriverName), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Update shipment location (in transit)
    /// </summary>
    [HttpPost("{id:guid}/location")]
    [ProducesResponseType(typeof(ShipmentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipmentOperationResult>> UpdateLocation(
        Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateShipmentLocationCommand(id, request.Location, request.Details), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Mark shipment as out for delivery
    /// </summary>
    [HttpPost("{id:guid}/out-for-delivery")]
    [ProducesResponseType(typeof(ShipmentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipmentOperationResult>> MarkOutForDelivery(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkOutForDeliveryCommand(id), cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Confirm delivery
    /// </summary>
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(ShipmentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipmentOperationResult>> ConfirmDelivery(
        Guid id,
        [FromBody] DeliveryConfirmationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ConfirmDeliveryCommand(id, request?.ReceiverName, request?.SignatureImage), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Record failed delivery attempt
    /// </summary>
    [HttpPost("{id:guid}/delivery-attempt")]
    [ProducesResponseType(typeof(ShipmentOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShipmentOperationResult>> RecordDeliveryAttempt(
        Guid id,
        [FromBody] DeliveryAttemptRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RecordDeliveryAttemptCommand(id, request.Reason), 
            cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Get order shipments
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(List<ShipmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ShipmentSummaryDto>>> GetOrderShipments(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderShipmentsQuery(orderId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get pending shipments (Admin)
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<ShipmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ShipmentSummaryDto>>> GetPendingShipments(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingShipmentsQuery(), cancellationToken);
        return Ok(result);
    }
}

// Request DTOs
public record CreateShipmentRequest(
    Guid OrderId,
    Guid CarrierId,
    string CarrierName,
    ShippingAddressDto ShippingAddress,
    ShipmentType ShipmentType = ShipmentType.Standard,
    string? DeliveryInstructions = null,
    List<ShipmentItemRequest>? Items = null);

public record ShippingAddressDto(
    string Street,
    string City,
    string Country,
    string PostalCode);

public record ShipmentItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Weight);

public record AssignTrackingRequest(string TrackingNumber);
public record PickupRequest(string? DriverName = null);
public record UpdateLocationRequest(string Location, string? Details = null);
public record DeliveryConfirmationRequest(string? ReceiverName = null, string? SignatureImage = null);
public record DeliveryAttemptRequest(string Reason);
