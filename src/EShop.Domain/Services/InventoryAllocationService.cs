using EShop.Domain.Aggregates.InventoryAggregate;
using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Services;

/// <summary>
/// Domain Service - Inventory Allocation
/// 
/// Handles complex inventory allocation logic that spans multiple aggregates:
/// - Multi-warehouse allocation strategies
/// - FIFO/LIFO allocation
/// - Reserve and commit flow
/// - Backorder handling
/// </summary>
public interface IInventoryAllocationService
{
    AllocationResult AllocateForOrder(
        Order order,
        IEnumerable<Warehouse> warehouses,
        AllocationStrategy strategy = AllocationStrategy.NearestWarehouse);

    void ReleaseAllocation(Guid orderId, IEnumerable<Warehouse> warehouses);
    bool CanFulfill(IEnumerable<AllocationRequest> requests, IEnumerable<Warehouse> warehouses);
}

public class InventoryAllocationService : IInventoryAllocationService
{
    public AllocationResult AllocateForOrder(
        Order order,
        IEnumerable<Warehouse> warehouses,
        AllocationStrategy strategy = AllocationStrategy.NearestWarehouse)
    {
        var warehouseList = warehouses.Where(w => w.IsActive).ToList();
        var result = new AllocationResult(order.Id);

        foreach (var orderLine in order.OrderLines)
        {
            var remainingQuantity = orderLine.Quantity;
            var allocations = new List<ItemAllocation>();

            // Try to allocate from warehouses based on strategy
            var orderedWarehouses = OrderWarehousesByStrategy(warehouseList, strategy);

            foreach (var warehouse in orderedWarehouses)
            {
                if (remainingQuantity <= 0) break;

                var availableStock = warehouse.GetTotalStockForProduct(orderLine.ProductId);
                if (availableStock <= 0) continue;

                var allocateQuantity = Math.Min(remainingQuantity, availableStock);

                // Find best location within warehouse (FIFO by default)
                var locationAllocations = AllocateFromWarehouse(
                    warehouse, 
                    orderLine.ProductId, 
                    allocateQuantity);

                allocations.AddRange(locationAllocations);
                remainingQuantity -= allocateQuantity;
            }

            if (remainingQuantity > 0)
            {
                // Partial allocation or backorder
                result.AddPartialAllocation(
                    orderLine.ProductId,
                    orderLine.ProductName,
                    orderLine.Quantity,
                    orderLine.Quantity - remainingQuantity,
                    allocations);
            }
            else
            {
                result.AddFullAllocation(
                    orderLine.ProductId,
                    orderLine.ProductName,
                    orderLine.Quantity,
                    allocations);
            }
        }

        return result;
    }

    public void ReleaseAllocation(Guid orderId, IEnumerable<Warehouse> warehouses)
    {
        // In a real implementation, this would track allocations in a separate store
        // and release reserved inventory back to available stock
    }

    public bool CanFulfill(IEnumerable<AllocationRequest> requests, IEnumerable<Warehouse> warehouses)
    {
        var warehouseList = warehouses.Where(w => w.IsActive).ToList();

        foreach (var request in requests)
        {
            var totalAvailable = warehouseList.Sum(w => w.GetTotalStockForProduct(request.ProductId));
            if (totalAvailable < request.Quantity)
                return false;
        }

        return true;
    }

    private IEnumerable<Warehouse> OrderWarehousesByStrategy(
        List<Warehouse> warehouses,
        AllocationStrategy strategy)
    {
        return strategy switch
        {
            AllocationStrategy.NearestWarehouse => warehouses.OrderBy(w => w.Type), // Simplified
            AllocationStrategy.LeastStock => warehouses.OrderBy(w => w.Locations.Sum(l => l.CurrentOccupancy)),
            AllocationStrategy.MostStock => warehouses.OrderByDescending(w => w.Locations.Sum(l => l.CurrentOccupancy)),
            AllocationStrategy.RoundRobin => warehouses, // Would need state tracking
            _ => warehouses
        };
    }

    private List<ItemAllocation> AllocateFromWarehouse(
        Warehouse warehouse,
        Guid productId,
        int quantity)
    {
        var allocations = new List<ItemAllocation>();
        var remainingQuantity = quantity;

        // Get locations with stock, ordered by zone type (picking zones first)
        var locationsWithStock = warehouse.GetStockLocationsForProduct(productId)
            .OrderBy(l => l.Location.Type == LocationType.Pickable ? 0 : 1)
            .ThenBy(l => l.Location.Code); // FIFO by location code

        foreach (var (location, stockQty) in locationsWithStock)
        {
            if (remainingQuantity <= 0) break;

            var allocateQty = Math.Min(remainingQuantity, stockQty);
            
            allocations.Add(new ItemAllocation(
                warehouse.Id,
                warehouse.Code,
                location.Id,
                location.Code,
                productId,
                allocateQty));

            remainingQuantity -= allocateQty;
        }

        return allocations;
    }
}

public class AllocationResult
{
    public Guid OrderId { get; }
    public bool IsFullyAllocated => !_lineAllocations.Any(l => l.IsPartial);
    public bool HasBackorders => _lineAllocations.Any(l => l.BackorderQuantity > 0);

    private readonly List<LineAllocation> _lineAllocations = new();
    public IReadOnlyCollection<LineAllocation> LineAllocations => _lineAllocations.AsReadOnly();

    public AllocationResult(Guid orderId)
    {
        OrderId = orderId;
    }

    public void AddFullAllocation(
        Guid productId,
        string productName,
        int quantity,
        List<ItemAllocation> allocations)
    {
        _lineAllocations.Add(new LineAllocation(
            productId, productName, quantity, quantity, 0, false, allocations));
    }

    public void AddPartialAllocation(
        Guid productId,
        string productName,
        int requestedQuantity,
        int allocatedQuantity,
        List<ItemAllocation> allocations)
    {
        _lineAllocations.Add(new LineAllocation(
            productId, 
            productName, 
            requestedQuantity, 
            allocatedQuantity, 
            requestedQuantity - allocatedQuantity,
            true,
            allocations));
    }
}

public class LineAllocation
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int RequestedQuantity { get; }
    public int AllocatedQuantity { get; }
    public int BackorderQuantity { get; }
    public bool IsPartial { get; }
    public IReadOnlyCollection<ItemAllocation> Allocations { get; }

    public LineAllocation(
        Guid productId,
        string productName,
        int requestedQuantity,
        int allocatedQuantity,
        int backorderQuantity,
        bool isPartial,
        List<ItemAllocation> allocations)
    {
        ProductId = productId;
        ProductName = productName;
        RequestedQuantity = requestedQuantity;
        AllocatedQuantity = allocatedQuantity;
        BackorderQuantity = backorderQuantity;
        IsPartial = isPartial;
        Allocations = allocations.AsReadOnly();
    }
}

public class ItemAllocation
{
    public Guid WarehouseId { get; }
    public string WarehouseCode { get; }
    public Guid LocationId { get; }
    public string LocationCode { get; }
    public Guid ProductId { get; }
    public int Quantity { get; }

    public ItemAllocation(
        Guid warehouseId,
        string warehouseCode,
        Guid locationId,
        string locationCode,
        Guid productId,
        int quantity)
    {
        WarehouseId = warehouseId;
        WarehouseCode = warehouseCode;
        LocationId = locationId;
        LocationCode = locationCode;
        ProductId = productId;
        Quantity = quantity;
    }
}

public class AllocationRequest
{
    public Guid ProductId { get; }
    public int Quantity { get; }

    public AllocationRequest(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}

public enum AllocationStrategy
{
    NearestWarehouse = 1,
    LeastStock = 2,
    MostStock = 3,
    RoundRobin = 4,
    CostOptimized = 5
}
