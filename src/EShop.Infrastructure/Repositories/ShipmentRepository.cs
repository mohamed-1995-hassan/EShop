using EShop.Domain.Aggregates.ShippingAggregate;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly EShopDbContext _context;

    public ShipmentRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Shipment>()
            .Include(s => s.Items)
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Shipment>()
            .Include(s => s.Items)
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Shipment>()
            .Where(s => s.OrderId == orderId)
            .Include(s => s.Items)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetByStatusAsync(ShipmentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Shipment>()
            .Where(s => s.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetPendingShipmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Shipment>()
            .Where(s => s.Status == ShipmentStatus.Pending || s.Status == ShipmentStatus.ReadyForPickup)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetInTransitShipmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Shipment>()
            .Where(s => s.Status == ShipmentStatus.InTransit || s.Status == ShipmentStatus.OutForDelivery)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetDeliveredTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Set<Shipment>()
            .Where(s => s.Status == ShipmentStatus.Delivered && 
                        s.ActualDeliveryDate != null && 
                        s.ActualDeliveryDate.Value.Date == today)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> GetOverdueShipmentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Shipment>()
            .Where(s => s.Status != ShipmentStatus.Delivered && 
                        s.Status != ShipmentStatus.Cancelled &&
                        s.EstimatedDeliveryDate < now)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await _context.Set<Shipment>().AddAsync(shipment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        _context.Set<Shipment>().Update(shipment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class CarrierRepository : ICarrierRepository
{
    private readonly EShopDbContext _context;

    public CarrierRepository(EShopDbContext context)
    {
        _context = context;
    }

    public async Task<Carrier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Carrier>()
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Carrier?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Carrier>()
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Carrier>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Carrier>()
            .Where(c => c.IsActive)
            .Include(c => c.Services)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Carrier>> GetByServiceTypeAsync(ShipmentType type, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Carrier>()
            .Where(c => c.IsActive && c.Services.Any(s => s.ShipmentType == type && s.IsActive))
            .Include(c => c.Services)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Carrier>> GetInternationalCarriersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Carrier>()
            .Where(c => c.IsActive && c.SupportsInternational)
            .Include(c => c.Services)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Carrier carrier, CancellationToken cancellationToken = default)
    {
        await _context.Set<Carrier>().AddAsync(carrier, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Carrier carrier, CancellationToken cancellationToken = default)
    {
        _context.Set<Carrier>().Update(carrier);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
