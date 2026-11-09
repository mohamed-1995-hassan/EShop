using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.InventoryAggregate;

/// <summary>
/// Warehouse Aggregate Root - Manages stock locations and inventory
/// 
/// BUSINESS RULES:
/// - Each warehouse has multiple stock locations (bins, shelves)
/// - Stock transfers between locations must maintain total
/// - Capacity limits per location
/// - Active/Inactive warehouse status
/// </summary>
public class Warehouse : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public WarehouseType Type { get; private set; }
    public bool IsActive { get; private set; }
    public int TotalCapacity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<StockLocation> _locations = new();
    public IReadOnlyCollection<StockLocation> Locations => _locations.AsReadOnly();

    private readonly List<WarehouseZone> _zones = new();
    public IReadOnlyCollection<WarehouseZone> Zones => _zones.AsReadOnly();

    private Warehouse() { }

    public static Warehouse Create(
        string code,
        string name,
        Address address,
        WarehouseType type,
        int totalCapacity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Warehouse code is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Warehouse name is required");
        if (totalCapacity <= 0)
            throw new DomainException("Capacity must be greater than zero");

        return new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            Name = name,
            Address = address,
            Type = type,
            TotalCapacity = totalCapacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public WarehouseZone AddZone(string name, ZoneType type, int capacity)
    {
        if (_zones.Any(z => z.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Zone '{name}' already exists");

        var zone = new WarehouseZone(Guid.NewGuid(), name, type, capacity);
        _zones.Add(zone);
        return zone;
    }

    public StockLocation AddLocation(
        string locationCode,
        Guid zoneId,
        int capacity,
        LocationType locationType = LocationType.Pickable)
    {
        var zone = _zones.FirstOrDefault(z => z.Id == zoneId)
            ?? throw new DomainException("Zone not found");

        if (_locations.Any(l => l.Code == locationCode))
            throw new DomainException($"Location '{locationCode}' already exists");

        var location = new StockLocation(Guid.NewGuid(), locationCode, zoneId, capacity, locationType);
        _locations.Add(location);
        return location;
    }

    public void PutAwayStock(Guid locationId, Guid productId, int quantity, string? lotNumber = null)
    {
        if (!IsActive)
            throw new DomainException("Cannot put away stock in inactive warehouse");

        var location = _locations.FirstOrDefault(l => l.Id == locationId)
            ?? throw new DomainException("Location not found");

        location.AddStock(productId, quantity, lotNumber);
    }

    public void PickStock(Guid locationId, Guid productId, int quantity)
    {
        if (!IsActive)
            throw new DomainException("Cannot pick stock from inactive warehouse");

        var location = _locations.FirstOrDefault(l => l.Id == locationId)
            ?? throw new DomainException("Location not found");

        location.RemoveStock(productId, quantity);
    }

    public void TransferStock(
        Guid fromLocationId,
        Guid toLocationId,
        Guid productId,
        int quantity)
    {
        var fromLocation = _locations.FirstOrDefault(l => l.Id == fromLocationId)
            ?? throw new DomainException("Source location not found");
        var toLocation = _locations.FirstOrDefault(l => l.Id == toLocationId)
            ?? throw new DomainException("Destination location not found");

        fromLocation.RemoveStock(productId, quantity);
        toLocation.AddStock(productId, quantity);
    }

    public int GetTotalStockForProduct(Guid productId)
    {
        return _locations.Sum(l => l.GetStockQuantity(productId));
    }

    public IEnumerable<(StockLocation Location, int Quantity)> GetStockLocationsForProduct(Guid productId)
    {
        return _locations
            .Select(l => (l, l.GetStockQuantity(productId)))
            .Where(x => x.Item2 > 0);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

public enum WarehouseType
{
    Main = 1,
    Distribution = 2,
    ReturnCenter = 3,
    DropShip = 4
}

public enum ZoneType
{
    Receiving = 1,
    Storage = 2,
    Picking = 3,
    Packing = 4,
    Shipping = 5,
    Returns = 6,
    Quarantine = 7
}

public enum LocationType
{
    Pickable = 1,
    Bulk = 2,
    Reserve = 3,
    Staging = 4,
    Damaged = 5
}
