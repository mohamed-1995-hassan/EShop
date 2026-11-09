using EShop.Domain.Aggregates.ShippingAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Shipments;

// ============================================
// GET SHIPMENT BY ID
// ============================================
public record GetShipmentQuery(Guid ShipmentId) : IRequest<ShipmentDetailsDto?>;

public record ShipmentDetailsDto(
    Guid Id,
    Guid OrderId,
    string TrackingNumber,
    string CarrierName,
    string Status,
    string ShipmentType,
    ShipmentAddressDto ShippingAddress,
    decimal Weight,
    decimal DeclaredValue,
    bool SignatureRequired,
    DateTime? EstimatedDeliveryDate,
    DateTime? ActualDeliveryDate,
    string? DeliveryInstructions,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    List<ShipmentItemDto> Items,
    List<TrackingEventDto> TrackingEvents,
    int DeliveryAttempts);

public record ShipmentAddressDto(
    string Street,
    string City,
    string Country,
    string PostalCode);

public record ShipmentItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Weight);

public record TrackingEventDto(
    Guid Id,
    string Status,
    string Details,
    DateTime Timestamp);

public class GetShipmentQueryHandler : IRequestHandler<GetShipmentQuery, ShipmentDetailsDto?>
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetShipmentQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ShipmentDetailsDto?> Handle(GetShipmentQuery request, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment == null) return null;

        return MapToDetailsDto(shipment);
    }

    private static ShipmentDetailsDto MapToDetailsDto(Shipment shipment)
    {
        return new ShipmentDetailsDto(
            shipment.Id,
            shipment.OrderId,
            shipment.TrackingNumber,
            shipment.CarrierName,
            shipment.Status.ToString(),
            shipment.Type.ToString(),
            new ShipmentAddressDto(
                shipment.ShippingAddress.Street,
                shipment.ShippingAddress.City,
                shipment.ShippingAddress.Country,
                shipment.ShippingAddress.PostalCode),
            shipment.Weight,
            shipment.DeclaredValue,
            shipment.SignatureRequired,
            shipment.EstimatedDeliveryDate,
            shipment.ActualDeliveryDate,
            shipment.DeliveryInstructions,
            shipment.CreatedAt,
            shipment.ShippedAt,
            shipment.Items.Select(i => new ShipmentItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.Weight)).ToList(),
            shipment.TrackingEvents.Select(e => new TrackingEventDto(
                e.Id,
                e.Status,
                e.Details,
                e.Timestamp)).ToList(),
            shipment.DeliveryAttempts.Count);
    }
}

// ============================================
// TRACK SHIPMENT BY TRACKING NUMBER
// ============================================
public record TrackShipmentQuery(string TrackingNumber) : IRequest<TrackingResultDto?>;

public record TrackingResultDto(
    string TrackingNumber,
    string CarrierName,
    string Status,
    DateTime? EstimatedDeliveryDate,
    DateTime? ActualDeliveryDate,
    ShipmentAddressDto DestinationAddress,
    List<TrackingEventDto> TrackingEvents);

public class TrackShipmentQueryHandler : IRequestHandler<TrackShipmentQuery, TrackingResultDto?>
{
    private readonly IShipmentRepository _shipmentRepository;

    public TrackShipmentQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<TrackingResultDto?> Handle(TrackShipmentQuery request, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentRepository.GetByTrackingNumberAsync(request.TrackingNumber, cancellationToken);
        if (shipment == null) return null;

        return new TrackingResultDto(
            shipment.TrackingNumber,
            shipment.CarrierName,
            shipment.Status.ToString(),
            shipment.EstimatedDeliveryDate,
            shipment.ActualDeliveryDate,
            new ShipmentAddressDto(
                shipment.ShippingAddress.Street,
                shipment.ShippingAddress.City,
                shipment.ShippingAddress.Country,
                shipment.ShippingAddress.PostalCode),
            shipment.TrackingEvents
                .OrderByDescending(e => e.Timestamp)
                .Select(e => new TrackingEventDto(e.Id, e.Status, e.Details, e.Timestamp))
                .ToList());
    }
}

// ============================================
// GET ORDER SHIPMENTS
// ============================================
public record GetOrderShipmentsQuery(Guid OrderId) : IRequest<List<ShipmentSummaryDto>>;

public record ShipmentSummaryDto(
    Guid Id,
    string TrackingNumber,
    string CarrierName,
    string Status,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt);

public class GetOrderShipmentsQueryHandler : IRequestHandler<GetOrderShipmentsQuery, List<ShipmentSummaryDto>>
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetOrderShipmentsQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<List<ShipmentSummaryDto>> Handle(GetOrderShipmentsQuery request, CancellationToken cancellationToken)
    {
        var shipments = await _shipmentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);

        return shipments.Select(s => new ShipmentSummaryDto(
            s.Id,
            s.TrackingNumber,
            s.CarrierName,
            s.Status.ToString(),
            s.Items.Count,
            s.CreatedAt,
            s.ShippedAt,
            s.ActualDeliveryDate)).ToList();
    }
}

// ============================================
// GET PENDING SHIPMENTS
// ============================================
public record GetPendingShipmentsQuery() : IRequest<List<ShipmentSummaryDto>>;

public class GetPendingShipmentsQueryHandler : IRequestHandler<GetPendingShipmentsQuery, List<ShipmentSummaryDto>>
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetPendingShipmentsQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<List<ShipmentSummaryDto>> Handle(GetPendingShipmentsQuery request, CancellationToken cancellationToken)
    {
        var shipments = await _shipmentRepository.GetPendingShipmentsAsync(cancellationToken);

        return shipments.Select(s => new ShipmentSummaryDto(
            s.Id,
            s.TrackingNumber,
            s.CarrierName,
            s.Status.ToString(),
            s.Items.Count,
            s.CreatedAt,
            s.ShippedAt,
            s.ActualDeliveryDate)).ToList();
    }
}
