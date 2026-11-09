using EShop.Domain.Aggregates.InventoryAggregate;
using EShop.Domain.Common;

namespace EShop.Domain.Events;

public class StockTransferCreatedEvent : DomainEvent
{
    public Guid TransferId { get; }
    public string TransferNumber { get; }
    public Guid SourceWarehouseId { get; }
    public Guid DestinationWarehouseId { get; }
    public TransferType Type { get; }

    public StockTransferCreatedEvent(Guid transferId, string transferNumber, Guid sourceWarehouseId, Guid destinationWarehouseId, TransferType type)
    {
        TransferId = transferId;
        TransferNumber = transferNumber;
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        Type = type;
    }
}

public class StockTransferApprovedEvent : DomainEvent
{
    public Guid TransferId { get; }
    public string TransferNumber { get; }
    public Guid ApprovedBy { get; }

    public StockTransferApprovedEvent(Guid transferId, string transferNumber, Guid approvedBy)
    {
        TransferId = transferId;
        TransferNumber = transferNumber;
        ApprovedBy = approvedBy;
    }
}

public class StockTransferShippedEvent : DomainEvent
{
    public Guid TransferId { get; }
    public string TransferNumber { get; }
    public Guid SourceWarehouseId { get; }
    public Guid DestinationWarehouseId { get; }

    public StockTransferShippedEvent(Guid transferId, string transferNumber, Guid sourceWarehouseId, Guid destinationWarehouseId)
    {
        TransferId = transferId;
        TransferNumber = transferNumber;
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
    }
}

public class StockTransferReceivedEvent : DomainEvent
{
    public Guid TransferId { get; }
    public string TransferNumber { get; }
    public bool HasVariance { get; }
    public IEnumerable<TransferLineVariance> Variances { get; }

    public StockTransferReceivedEvent(Guid transferId, string transferNumber, bool hasVariance, IEnumerable<TransferLineVariance> variances)
    {
        TransferId = transferId;
        TransferNumber = transferNumber;
        HasVariance = hasVariance;
        Variances = variances;
    }
}

public class TransferLineVariance
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Expected { get; }
    public int Received { get; }
    public int Variance { get; }

    public TransferLineVariance(Guid productId, string productName, int expected, int received, int variance)
    {
        ProductId = productId;
        ProductName = productName;
        Expected = expected;
        Received = received;
        Variance = variance;
    }
}

public class StockPutAwayEvent : DomainEvent
{
    public Guid WarehouseId { get; }
    public Guid LocationId { get; }
    public string LocationCode { get; }
    public Guid ProductId { get; }
    public int Quantity { get; }
    public string? LotNumber { get; }

    public StockPutAwayEvent(Guid warehouseId, Guid locationId, string locationCode, Guid productId, int quantity, string? lotNumber)
    {
        WarehouseId = warehouseId;
        LocationId = locationId;
        LocationCode = locationCode;
        ProductId = productId;
        Quantity = quantity;
        LotNumber = lotNumber;
    }
}

public class StockPickedEvent : DomainEvent
{
    public Guid WarehouseId { get; }
    public Guid LocationId { get; }
    public string LocationCode { get; }
    public Guid ProductId { get; }
    public int Quantity { get; }
    public Guid? OrderId { get; }

    public StockPickedEvent(Guid warehouseId, Guid locationId, string locationCode, Guid productId, int quantity, Guid? orderId)
    {
        WarehouseId = warehouseId;
        LocationId = locationId;
        LocationCode = locationCode;
        ProductId = productId;
        Quantity = quantity;
        OrderId = orderId;
    }
}

public class LocationBlockedEvent : DomainEvent
{
    public Guid WarehouseId { get; }
    public Guid LocationId { get; }
    public string LocationCode { get; }
    public string Reason { get; }

    public LocationBlockedEvent(Guid warehouseId, Guid locationId, string locationCode, string reason)
    {
        WarehouseId = warehouseId;
        LocationId = locationId;
        LocationCode = locationCode;
        Reason = reason;
    }
}

public class InventoryCycleCountCompletedEvent : DomainEvent
{
    public Guid WarehouseId { get; }
    public Guid LocationId { get; }
    public IEnumerable<CycleCountVariance> Variances { get; }

    public InventoryCycleCountCompletedEvent(Guid warehouseId, Guid locationId, IEnumerable<CycleCountVariance> variances)
    {
        WarehouseId = warehouseId;
        LocationId = locationId;
        Variances = variances;
    }
}

public class CycleCountVariance
{
    public Guid ProductId { get; }
    public int SystemQuantity { get; }
    public int CountedQuantity { get; }
    public int Variance { get; }

    public CycleCountVariance(Guid productId, int systemQuantity, int countedQuantity, int variance)
    {
        ProductId = productId;
        SystemQuantity = systemQuantity;
        CountedQuantity = countedQuantity;
        Variance = variance;
    }
}
