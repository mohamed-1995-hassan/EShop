using EShop.Domain.Aggregates.ShippingAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Shipments;

// ============================================
// DTOs
// ============================================
public record CreateShipmentAddressDto(
    string Street,
    string City,
    string Country,
    string PostalCode);

public record CreateShipmentItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Weight);

// ============================================
// CREATE SHIPMENT
// ============================================
public record CreateShipmentCommand(
    Guid OrderId,
    Guid CarrierId,
    string CarrierName,
    CreateShipmentAddressDto ShippingAddress,
    ShipmentType ShipmentType = ShipmentType.Standard,
    string? DeliveryInstructions = null,
    List<CreateShipmentItemDto>? Items = null) : IRequest<CreateShipmentResult>;

public record CreateShipmentResult(
    bool Success,
    Guid? ShipmentId,
    string? ErrorMessage);

public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, CreateShipmentResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public CreateShipmentCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<CreateShipmentResult> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var address = new Address(
                request.ShippingAddress.Street,
                request.ShippingAddress.City,
                request.ShippingAddress.Country,
                request.ShippingAddress.PostalCode);

            var shipment = Shipment.Create(
                request.OrderId,
                request.CarrierId,
                request.CarrierName,
                address,
                request.ShipmentType,
                request.DeliveryInstructions);

            if (request.Items != null)
            {
                foreach (var item in request.Items)
                {
                    shipment.AddItem(item.ProductId, item.ProductName, item.Quantity, item.Weight);
                }
            }

            await _shipmentRepository.AddAsync(shipment, cancellationToken);

            return new CreateShipmentResult(true, shipment.Id, null);
        }
        catch (Exception ex)
        {
            return new CreateShipmentResult(false, null, ex.Message);
        }
    }
}

// Note: CreateShipmentItemDto.Weight is decimal which matches the expected type

// ============================================
// ASSIGN TRACKING NUMBER
// ============================================
public record AssignTrackingNumberCommand(Guid ShipmentId, string TrackingNumber) : IRequest<ShipmentOperationResult>;

public record ShipmentOperationResult(bool Success, string? Status, string? ErrorMessage);

public class AssignTrackingNumberCommandHandler : IRequestHandler<AssignTrackingNumberCommand, ShipmentOperationResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public AssignTrackingNumberCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentOperationResult> Handle(AssignTrackingNumberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return new ShipmentOperationResult(false, null, "Shipment not found");

            shipment.AssignTrackingNumber(request.TrackingNumber);
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            return new ShipmentOperationResult(true, shipment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ShipmentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// MARK READY FOR PICKUP
// ============================================
public record MarkReadyForPickupCommand(Guid ShipmentId) : IRequest<ShipmentOperationResult>;

public class MarkReadyForPickupCommandHandler : IRequestHandler<MarkReadyForPickupCommand, ShipmentOperationResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public MarkReadyForPickupCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentOperationResult> Handle(MarkReadyForPickupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return new ShipmentOperationResult(false, null, "Shipment not found");

            shipment.MarkAsReadyForPickup();
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            return new ShipmentOperationResult(true, shipment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ShipmentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// MARK PICKED UP
// ============================================
public record MarkPickedUpCommand(Guid ShipmentId, string? DriverName = null) : IRequest<ShipmentOperationResult>;

public class MarkPickedUpCommandHandler : IRequestHandler<MarkPickedUpCommand, ShipmentOperationResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public MarkPickedUpCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentOperationResult> Handle(MarkPickedUpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return new ShipmentOperationResult(false, null, "Shipment not found");

            shipment.MarkAsPickedUp(request.DriverName);
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            return new ShipmentOperationResult(true, shipment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ShipmentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// UPDATE LOCATION (IN TRANSIT)
// ============================================
public record UpdateShipmentLocationCommand(Guid ShipmentId, string Location, string? Details = null) : IRequest<ShipmentOperationResult>;

public class UpdateShipmentLocationCommandHandler : IRequestHandler<UpdateShipmentLocationCommand, ShipmentOperationResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public UpdateShipmentLocationCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentOperationResult> Handle(UpdateShipmentLocationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return new ShipmentOperationResult(false, null, "Shipment not found");

            shipment.MarkAsInTransit(request.Location, request.Details);
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            return new ShipmentOperationResult(true, shipment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ShipmentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// MARK OUT FOR DELIVERY
// ============================================
public record MarkOutForDeliveryCommand(Guid ShipmentId) : IRequest<ShipmentOperationResult>;

public class MarkOutForDeliveryCommandHandler : IRequestHandler<MarkOutForDeliveryCommand, ShipmentOperationResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public MarkOutForDeliveryCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentOperationResult> Handle(MarkOutForDeliveryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return new ShipmentOperationResult(false, null, "Shipment not found");

            shipment.MarkAsOutForDelivery();
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            return new ShipmentOperationResult(true, shipment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ShipmentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// CONFIRM DELIVERY
// ============================================
public record ConfirmDeliveryCommand(Guid ShipmentId, string? ReceiverName = null, string? SignatureImage = null) : IRequest<ShipmentOperationResult>;

public class ConfirmDeliveryCommandHandler : IRequestHandler<ConfirmDeliveryCommand, ShipmentOperationResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public ConfirmDeliveryCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentOperationResult> Handle(ConfirmDeliveryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return new ShipmentOperationResult(false, null, "Shipment not found");

            shipment.MarkAsDelivered(request.ReceiverName, request.SignatureImage);
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            return new ShipmentOperationResult(true, shipment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ShipmentOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// RECORD DELIVERY ATTEMPT
// ============================================
public record RecordDeliveryAttemptCommand(Guid ShipmentId, string Reason) : IRequest<ShipmentOperationResult>;

public class RecordDeliveryAttemptCommandHandler : IRequestHandler<RecordDeliveryAttemptCommand, ShipmentOperationResult>
{
    private readonly IShipmentRepository _shipmentRepository;

    public RecordDeliveryAttemptCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentOperationResult> Handle(RecordDeliveryAttemptCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
            if (shipment == null)
                return new ShipmentOperationResult(false, null, "Shipment not found");

            shipment.RecordDeliveryAttempt(request.Reason);
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            return new ShipmentOperationResult(true, shipment.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ShipmentOperationResult(false, null, ex.Message);
        }
    }
}
