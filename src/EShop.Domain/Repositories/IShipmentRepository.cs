using EShop.Domain.Aggregates.ShippingAggregate;

namespace EShop.Domain.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetByStatusAsync(ShipmentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetPendingShipmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetInTransitShipmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetDeliveredTodayAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetOverdueShipmentsAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default);
}

public interface ICarrierRepository
{
    Task<Carrier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Carrier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Carrier>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Carrier>> GetByServiceTypeAsync(ShipmentType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Carrier>> GetInternationalCarriersAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(Carrier carrier, CancellationToken cancellationToken = default);
    Task UpdateAsync(Carrier carrier, CancellationToken cancellationToken = default);
}
