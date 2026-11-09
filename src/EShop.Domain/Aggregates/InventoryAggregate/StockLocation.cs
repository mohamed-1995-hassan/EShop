using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.InventoryAggregate;

/// <summary>
/// Stock Location Entity - A bin/shelf within a warehouse
/// </summary>
public class StockLocation : Entity<Guid>
{
    public string Code { get; private set; }
    public Guid ZoneId { get; private set; }
    public int Capacity { get; private set; }
    public LocationType Type { get; private set; }
    public bool IsBlocked { get; private set; }

    private readonly List<LocationInventory> _inventory = new();
    public IReadOnlyCollection<LocationInventory> Inventory => _inventory.AsReadOnly();

    internal StockLocation(Guid id, string code, Guid zoneId, int capacity, LocationType type)
    {
        Id = id;
        Code = code;
        ZoneId = zoneId;
        Capacity = capacity;
        Type = type;
        IsBlocked = false;
    }

    private StockLocation() { } // EF Core

    public int CurrentOccupancy => _inventory.Sum(i => i.Quantity);
    public int AvailableCapacity => Capacity - CurrentOccupancy;

    public void AddStock(Guid productId, int quantity, string? lotNumber = null)
    {
        if (IsBlocked)
            throw new DomainException("Location is blocked");

        if (quantity > AvailableCapacity)
            throw new DomainException($"Insufficient capacity. Available: {AvailableCapacity}, Requested: {quantity}");

        var existing = _inventory.FirstOrDefault(i => 
            i.ProductId == productId && i.LotNumber == lotNumber);

        if (existing != null)
        {
            existing.IncreaseQuantity(quantity);
        }
        else
        {
            _inventory.Add(new LocationInventory(productId, quantity, lotNumber));
        }
    }

    public void RemoveStock(Guid productId, int quantity)
    {
        var inventoryItem = _inventory.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new DomainException($"Product not found in location {Code}");

        if (inventoryItem.Quantity < quantity)
            throw new DomainException($"Insufficient quantity. Available: {inventoryItem.Quantity}");

        inventoryItem.DecreaseQuantity(quantity);

        if (inventoryItem.Quantity == 0)
            _inventory.Remove(inventoryItem);
    }

    public int GetStockQuantity(Guid productId)
    {
        return _inventory
            .Where(i => i.ProductId == productId)
            .Sum(i => i.Quantity);
    }

    public void Block(string reason) => IsBlocked = true;
    public void Unblock() => IsBlocked = false;
}

/// <summary>
/// Inventory at a specific location
/// </summary>
public class LocationInventory
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public string? LotNumber { get; private set; }
    public DateTime LastUpdated { get; private set; }

    internal LocationInventory(Guid productId, int quantity, string? lotNumber = null)
    {
        ProductId = productId;
        Quantity = quantity;
        LotNumber = lotNumber;
        LastUpdated = DateTime.UtcNow;
    }

    private LocationInventory() { } // EF Core

    internal void IncreaseQuantity(int amount)
    {
        Quantity += amount;
        LastUpdated = DateTime.UtcNow;
    }

    internal void DecreaseQuantity(int amount)
    {
        Quantity -= amount;
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Warehouse Zone - Logical grouping of locations
/// </summary>
public class WarehouseZone : Entity<Guid>
{
    public string Name { get; private set; }
    public ZoneType Type { get; private set; }
    public int Capacity { get; private set; }

    internal WarehouseZone(Guid id, string name, ZoneType type, int capacity)
    {
        Id = id;
        Name = name;
        Type = type;
        Capacity = capacity;
    }

    private WarehouseZone() { } // EF Core
}
