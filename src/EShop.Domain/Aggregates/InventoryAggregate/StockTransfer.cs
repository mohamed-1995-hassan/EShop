using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.InventoryAggregate;

/// <summary>
/// Stock Transfer Aggregate - Tracks movement between warehouses
/// 
/// BUSINESS RULES:
/// - Must be approved before shipping
/// - Cannot modify after shipped
/// - Receiving validates against expected quantities
/// - Partial receives allowed with variance tracking
/// </summary>
public class StockTransfer : AggregateRoot<Guid>
{
    public string TransferNumber { get; private set; } = string.Empty;
    public Guid SourceWarehouseId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public TransferStatus Status { get; private set; }
    public TransferType Type { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }

    private readonly List<TransferLine> _lines = new();
    public IReadOnlyCollection<TransferLine> Lines => _lines.AsReadOnly();

    private StockTransfer() { }

    public static StockTransfer Create(
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        TransferType type,
        Guid createdBy,
        string? notes = null)
    {
        if (sourceWarehouseId == destinationWarehouseId)
            throw new DomainException("Source and destination warehouses must be different");

        return new StockTransfer
        {
            Id = Guid.NewGuid(),
            TransferNumber = GenerateTransferNumber(),
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            Type = type,
            Status = TransferStatus.Draft,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void AddLine(Guid productId, string productName, int quantity)
    {
        if (Status != TransferStatus.Draft)
            throw new DomainException("Cannot modify transfer after submission");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        var existingLine = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existingLine != null)
        {
            existingLine.UpdateQuantity(existingLine.RequestedQuantity + quantity);
        }
        else
        {
            _lines.Add(new TransferLine(Guid.NewGuid(), productId, productName, quantity));
        }
    }

    public void RemoveLine(Guid lineId)
    {
        if (Status != TransferStatus.Draft)
            throw new DomainException("Cannot modify transfer after submission");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException("Line not found");

        _lines.Remove(line);
    }

    public void Submit()
    {
        if (Status != TransferStatus.Draft)
            throw new DomainException("Transfer already submitted");

        if (!_lines.Any())
            throw new DomainException("Cannot submit empty transfer");

        Status = TransferStatus.PendingApproval;
    }

    public void Approve(Guid approvedBy)
    {
        if (Status != TransferStatus.PendingApproval)
            throw new DomainException("Transfer not pending approval");

        ApprovedBy = approvedBy;
        Status = TransferStatus.Approved;
    }

    public void Reject(string reason)
    {
        if (Status != TransferStatus.PendingApproval)
            throw new DomainException("Transfer not pending approval");

        Notes = $"Rejected: {reason}";
        Status = TransferStatus.Rejected;
    }

    public void Ship()
    {
        if (Status != TransferStatus.Approved)
            throw new DomainException("Transfer must be approved before shipping");

        ShippedAt = DateTime.UtcNow;
        Status = TransferStatus.InTransit;
    }

    public void Receive(IEnumerable<(Guid LineId, int ReceivedQuantity)> receivedItems)
    {
        if (Status != TransferStatus.InTransit)
            throw new DomainException("Transfer not in transit");

        foreach (var (lineId, receivedQuantity) in receivedItems)
        {
            var line = _lines.FirstOrDefault(l => l.Id == lineId)
                ?? throw new DomainException("Line not found");

            line.Receive(receivedQuantity);
        }

        ReceivedAt = DateTime.UtcNow;
        Status = _lines.Any(l => l.Variance != 0) 
            ? TransferStatus.ReceivedWithVariance 
            : TransferStatus.Completed;
    }

    public void Cancel(string reason)
    {
        if (Status == TransferStatus.InTransit || Status == TransferStatus.Completed)
            throw new DomainException("Cannot cancel transfer in transit or completed");

        Notes = $"Cancelled: {reason}";
        Status = TransferStatus.Cancelled;
    }

    private static string GenerateTransferNumber()
    {
        return $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

public class TransferLine : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int RequestedQuantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public int Variance => ReceivedQuantity - RequestedQuantity;

    internal TransferLine(Guid id, Guid productId, string productName, int quantity)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        RequestedQuantity = quantity;
        ReceivedQuantity = 0;
    }

    private TransferLine() { } // EF Core

    internal void UpdateQuantity(int quantity) => RequestedQuantity = quantity;
    internal void Receive(int quantity) => ReceivedQuantity = quantity;
}

public enum TransferStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    InTransit = 5,
    ReceivedWithVariance = 6,
    Completed = 7,
    Cancelled = 8
}

public enum TransferType
{
    Replenishment = 1,
    Rebalancing = 2,
    ReturnToVendor = 3,
    Consolidation = 4
}
