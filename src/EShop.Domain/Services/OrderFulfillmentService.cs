using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Aggregates.PaymentAggregate;
using EShop.Domain.Aggregates.ShippingAggregate;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Services;

/// <summary>
/// Domain Service - Order Fulfillment Orchestration
/// 
/// Coordinates complex order fulfillment logic across multiple aggregates:
/// - Payment verification
/// - Inventory reservation
/// - Shipment creation
/// - Split shipment handling
/// </summary>
public interface IOrderFulfillmentService
{
    FulfillmentResult ProcessFulfillment(
        Order order,
        Payment payment,
        AllocationResult allocation,
        IEnumerable<Carrier> carriers,
        Address shippingAddress);

    bool CanFulfill(Order order, Payment payment, AllocationResult allocation);
    FulfillmentPlan CreateFulfillmentPlan(Order order, AllocationResult allocation);
}

public class OrderFulfillmentService : IOrderFulfillmentService
{
    public FulfillmentResult ProcessFulfillment(
        Order order,
        Payment payment,
        AllocationResult allocation,
        IEnumerable<Carrier> carriers,
        Address shippingAddress)
    {
        // Validate prerequisites
        ValidateFulfillmentPrerequisites(order, payment, allocation);

        var result = new FulfillmentResult(order.Id);
        var carrierList = carriers.Where(c => c.IsActive).ToList();

        // Group allocations by warehouse for shipment creation
        var warehouseGroups = allocation.LineAllocations
            .SelectMany(la => la.Allocations)
            .GroupBy(a => a.WarehouseId);

        foreach (var warehouseGroup in warehouseGroups)
        {
            // Select best carrier for this shipment
            var carrier = SelectCarrier(carrierList, ShipmentType.Standard);
            if (carrier == null)
            {
                result.AddError($"No carrier available for warehouse {warehouseGroup.Key}");
                continue;
            }

            // Create shipment for this warehouse's items
            var shipment = Shipment.Create(
                order.Id,
                carrier.Id,
                carrier.Name,
                shippingAddress,
                ShipmentType.Standard);

            // Add items to shipment
            foreach (var itemAllocation in warehouseGroup)
            {
                var orderLine = order.OrderLines.FirstOrDefault(ol => ol.ProductId == itemAllocation.ProductId);
                if (orderLine != null)
                {
                    shipment.AddItem(
                        itemAllocation.ProductId,
                        orderLine.ProductName,
                        itemAllocation.Quantity,
                        0.5m); // Default weight - would come from product
                }
            }

            // Calculate shipping cost
            var shippingCost = carrier.CalculateShippingCost(
                ShipmentType.Standard,
                shipment.Weight,
                shippingAddress.Country);

            shipment.SetDeclaredValue(order.TotalAmount.Amount);
            shipment.SetEstimatedDelivery(CalculateEstimatedDelivery(carrier, ShipmentType.Standard));

            result.AddShipment(shipment, warehouseGroup.First().WarehouseCode);
        }

        // Handle backorders if any
        if (allocation.HasBackorders)
        {
            foreach (var backorder in allocation.LineAllocations.Where(la => la.BackorderQuantity > 0))
            {
                result.AddBackorder(
                    backorder.ProductId,
                    backorder.ProductName,
                    backorder.BackorderQuantity);
            }
        }

        return result;
    }

    public bool CanFulfill(Order order, Payment payment, AllocationResult allocation)
    {
        if (order.Status != OrderStatus.Placed && order.Status != OrderStatus.Confirmed)
            return false;

        if (payment.Status != PaymentStatus.Captured)
            return false;

        if (!allocation.IsFullyAllocated)
            return false;

        return true;
    }

    public FulfillmentPlan CreateFulfillmentPlan(Order order, AllocationResult allocation)
    {
        var plan = new FulfillmentPlan(order.Id);

        // Group by warehouse
        var warehouseGroups = allocation.LineAllocations
            .SelectMany(la => la.Allocations.Select(a => new { LineAllocation = la, Allocation = a }))
            .GroupBy(x => x.Allocation.WarehouseId);

        foreach (var warehouseGroup in warehouseGroups)
        {
            var shipmentPlan = new ShipmentPlan(
                warehouseGroup.Key,
                warehouseGroup.First().Allocation.WarehouseCode);

            foreach (var item in warehouseGroup)
            {
                shipmentPlan.AddItem(
                    item.Allocation.ProductId,
                    item.LineAllocation.ProductName,
                    item.Allocation.Quantity,
                    item.Allocation.LocationCode);
            }

            plan.AddShipmentPlan(shipmentPlan);
        }

        // Add backorder items
        foreach (var line in allocation.LineAllocations.Where(la => la.BackorderQuantity > 0))
        {
            plan.AddBackorderItem(line.ProductId, line.ProductName, line.BackorderQuantity);
        }

        return plan;
    }

    private void ValidateFulfillmentPrerequisites(
        Order order,
        Payment payment,
        AllocationResult allocation)
    {
        if (order.Status != OrderStatus.Placed && order.Status != OrderStatus.Confirmed)
            throw new DomainException("Order must be placed or confirmed for fulfillment");

        if (payment.Status != PaymentStatus.Captured)
            throw new DomainException("Payment must be captured before fulfillment");

        if (!allocation.LineAllocations.Any())
            throw new DomainException("No inventory allocated for order");
    }

    private Carrier? SelectCarrier(List<Carrier> carriers, ShipmentType shipmentType)
    {
        // Simple carrier selection - could be extended with cost/time optimization
        return carriers.FirstOrDefault(c => c.GetService(shipmentType) != null);
    }

    private DateTime CalculateEstimatedDelivery(Carrier carrier, ShipmentType shipmentType)
    {
        var service = carrier.GetService(shipmentType);
        if (service == null)
            return DateTime.UtcNow.AddDays(7);

        return DateTime.UtcNow.AddDays(service.EstimatedDaysMax);
    }
}

public class FulfillmentResult
{
    public Guid OrderId { get; }
    public bool IsSuccessful => !_errors.Any();
    public bool HasBackorders => _backorders.Any();
    public bool IsSplitShipment => _shipments.Count > 1;

    private readonly List<(Shipment Shipment, string WarehouseCode)> _shipments = new();
    public IReadOnlyCollection<(Shipment Shipment, string WarehouseCode)> Shipments => _shipments.AsReadOnly();

    private readonly List<BackorderItem> _backorders = new();
    public IReadOnlyCollection<BackorderItem> Backorders => _backorders.AsReadOnly();

    private readonly List<string> _errors = new();
    public IReadOnlyCollection<string> Errors => _errors.AsReadOnly();

    public FulfillmentResult(Guid orderId)
    {
        OrderId = orderId;
    }

    public void AddShipment(Shipment shipment, string warehouseCode)
    {
        _shipments.Add((shipment, warehouseCode));
    }

    public void AddBackorder(Guid productId, string productName, int quantity)
    {
        _backorders.Add(new BackorderItem(productId, productName, quantity));
    }

    public void AddError(string error)
    {
        _errors.Add(error);
    }
}

public class BackorderItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }

    public BackorderItem(Guid productId, string productName, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
    }
}

public class FulfillmentPlan
{
    public Guid OrderId { get; }
    public bool RequiresSplitShipment => _shipmentPlans.Count > 1;
    public bool HasBackorders => _backorderItems.Any();

    private readonly List<ShipmentPlan> _shipmentPlans = new();
    public IReadOnlyCollection<ShipmentPlan> ShipmentPlans => _shipmentPlans.AsReadOnly();

    private readonly List<BackorderItem> _backorderItems = new();
    public IReadOnlyCollection<BackorderItem> BackorderItems => _backorderItems.AsReadOnly();

    public FulfillmentPlan(Guid orderId)
    {
        OrderId = orderId;
    }

    public void AddShipmentPlan(ShipmentPlan plan)
    {
        _shipmentPlans.Add(plan);
    }

    public void AddBackorderItem(Guid productId, string productName, int quantity)
    {
        _backorderItems.Add(new BackorderItem(productId, productName, quantity));
    }
}

public class ShipmentPlan
{
    public Guid WarehouseId { get; }
    public string WarehouseCode { get; }

    private readonly List<ShipmentPlanItem> _items = new();
    public IReadOnlyCollection<ShipmentPlanItem> Items => _items.AsReadOnly();

    public ShipmentPlan(Guid warehouseId, string warehouseCode)
    {
        WarehouseId = warehouseId;
        WarehouseCode = warehouseCode;
    }

    public void AddItem(Guid productId, string productName, int quantity, string locationCode)
    {
        _items.Add(new ShipmentPlanItem(productId, productName, quantity, locationCode));
    }
}

public class ShipmentPlanItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public string LocationCode { get; }

    public ShipmentPlanItem(Guid productId, string productName, int quantity, string locationCode)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        LocationCode = locationCode;
    }
}
