using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.ReturnAggregate;

/// <summary>
/// Return Request Aggregate - Handles product returns and exchanges
/// 
/// BUSINESS RULES:
/// - Returns allowed within 30 days of delivery
/// - Refund after inspection passes
/// - Exchange for same or different item
/// - Return shipping label generation
/// - Quality inspection workflow
/// - Partial returns supported
/// </summary>
public class ReturnRequest : AggregateRoot<Guid>
{
    public string ReturnNumber { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public ReturnStatus Status { get; private set; }
    public ReturnType Type { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? CustomerNotes { get; private set; }
    public string? InternalNotes { get; private set; }
    public Money RefundAmount { get; private set; } = null!;
    public Money? RestockingFee { get; private set; }
    public RefundMethod PreferredRefundMethod { get; private set; }
    public string? ReturnTrackingNumber { get; private set; }
    public string? ReturnLabelUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public DateTime? InspectedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? ProcessedBy { get; private set; }

    private readonly List<ReturnItem> _items = new();
    public IReadOnlyCollection<ReturnItem> Items => _items.AsReadOnly();

    private readonly List<ReturnImage> _images = new();
    public IReadOnlyCollection<ReturnImage> Images => _images.AsReadOnly();

    private readonly List<ReturnStatusHistory> _statusHistory = new();
    public IReadOnlyCollection<ReturnStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public bool CanCancel => Status == ReturnStatus.Pending || Status == ReturnStatus.Approved;
    public bool RequiresInspection => _items.Any(i => i.RequiresInspection);

    private ReturnRequest() { }

    public static ReturnRequest Create(
        Guid orderId,
        Guid customerId,
        ReturnType type,
        string reason,
        RefundMethod preferredRefundMethod,
        string? customerNotes = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("Order ID is required");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Return reason is required");

        var returnRequest = new ReturnRequest
        {
            Id = Guid.NewGuid(),
            ReturnNumber = GenerateReturnNumber(),
            OrderId = orderId,
            CustomerId = customerId,
            Type = type,
            Reason = reason,
            CustomerNotes = customerNotes,
            PreferredRefundMethod = preferredRefundMethod,
            Status = ReturnStatus.Pending,
            RefundAmount = Money.Zero("KWD"),
            CreatedAt = DateTime.UtcNow
        };

        returnRequest.AddStatusHistory(ReturnStatus.Pending, "Return request created");

        return returnRequest;
    }

    #region Item Management

    public ReturnItem AddItem(
        Guid orderLineId,
        Guid productId,
        string productName,
        int quantity,
        Money unitPrice,
        ReturnReason itemReason,
        string? reasonDetails = null)
    {
        if (Status != ReturnStatus.Pending)
            throw new DomainException("Cannot modify return after submission");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        var existingItem = _items.FirstOrDefault(i => i.OrderLineId == orderLineId);
        if (existingItem != null)
            throw new DomainException("Item already added to return");

        var item = new ReturnItem(
            Guid.NewGuid(),
            orderLineId,
            productId,
            productName,
            quantity,
            unitPrice,
            itemReason,
            reasonDetails);

        _items.Add(item);
        RecalculateRefundAmount();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        if (Status != ReturnStatus.Pending)
            throw new DomainException("Cannot modify return after submission");

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found");

        _items.Remove(item);
        RecalculateRefundAmount();
    }

    public void AddImage(string imageUrl, string? description = null)
    {
        if (_images.Count >= 10)
            throw new DomainException("Maximum 10 images allowed");

        _images.Add(new ReturnImage(Guid.NewGuid(), imageUrl, description));
    }

    #endregion

    #region Workflow

    public void Submit()
    {
        if (Status != ReturnStatus.Pending)
            throw new DomainException("Return already submitted");

        if (!_items.Any())
            throw new DomainException("At least one item required");

        Status = ReturnStatus.PendingApproval;
        AddStatusHistory(ReturnStatus.PendingApproval, "Return submitted for approval");
    }

    public void Approve(Guid approvedBy, string? notes = null)
    {
        if (Status != ReturnStatus.PendingApproval)
            throw new DomainException("Return not pending approval");

        Status = ReturnStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ProcessedBy = approvedBy;
        if (!string.IsNullOrWhiteSpace(notes))
            InternalNotes = notes;

        AddStatusHistory(ReturnStatus.Approved, "Return approved");
    }

    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status != ReturnStatus.PendingApproval)
            throw new DomainException("Return not pending approval");

        Status = ReturnStatus.Rejected;
        ProcessedBy = rejectedBy;
        InternalNotes = $"Rejected: {reason}";

        AddStatusHistory(ReturnStatus.Rejected, reason);
    }

    public void GenerateReturnLabel(string trackingNumber, string labelUrl)
    {
        if (Status != ReturnStatus.Approved)
            throw new DomainException("Return must be approved first");

        ReturnTrackingNumber = trackingNumber;
        ReturnLabelUrl = labelUrl;
        Status = ReturnStatus.LabelGenerated;

        AddStatusHistory(ReturnStatus.LabelGenerated, $"Return label generated: {trackingNumber}");
    }

    public void MarkAsShipped()
    {
        if (Status != ReturnStatus.LabelGenerated && Status != ReturnStatus.Approved)
            throw new DomainException("Invalid status for shipping");

        Status = ReturnStatus.InTransit;
        AddStatusHistory(ReturnStatus.InTransit, "Return shipment in transit");
    }

    public void MarkAsReceived()
    {
        if (Status != ReturnStatus.InTransit)
            throw new DomainException("Return must be in transit");

        Status = ReturnStatus.Received;
        ReceivedAt = DateTime.UtcNow;
        AddStatusHistory(ReturnStatus.Received, "Return received at warehouse");
    }

    public void StartInspection()
    {
        if (Status != ReturnStatus.Received)
            throw new DomainException("Return must be received first");

        Status = ReturnStatus.Inspecting;
        AddStatusHistory(ReturnStatus.Inspecting, "Quality inspection started");
    }

    public void RecordInspectionResult(
        Guid itemId, 
        InspectionResult result, 
        string? notes = null)
    {
        if (Status != ReturnStatus.Inspecting)
            throw new DomainException("Return must be in inspection");

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item not found");

        item.SetInspectionResult(result, notes);
    }

    public void CompleteInspection()
    {
        if (Status != ReturnStatus.Inspecting)
            throw new DomainException("Return must be in inspection");

        if (_items.Any(i => i.InspectionResult == null))
            throw new DomainException("All items must be inspected");

        InspectedAt = DateTime.UtcNow;

        // Calculate final refund based on inspection results
        RecalculateRefundAmount();

        // Determine if any items failed inspection
        var anyFailed = _items.Any(i => i.InspectionResult == InspectionResult.Failed);
        
        if (anyFailed)
        {
            Status = ReturnStatus.PartiallyApproved;
            AddStatusHistory(ReturnStatus.PartiallyApproved, "Inspection completed with partial approval");
        }
        else
        {
            Status = ReturnStatus.InspectionPassed;
            AddStatusHistory(ReturnStatus.InspectionPassed, "Inspection passed");
        }
    }

    public void ProcessRefund(Guid processedBy)
    {
        if (Status != ReturnStatus.InspectionPassed && Status != ReturnStatus.PartiallyApproved)
            throw new DomainException("Return must pass inspection");

        Status = ReturnStatus.RefundProcessed;
        ProcessedBy = processedBy;
        AddStatusHistory(ReturnStatus.RefundProcessed, $"Refund of {RefundAmount} processed");
    }

    public void Complete()
    {
        if (Status != ReturnStatus.RefundProcessed)
            throw new DomainException("Refund must be processed first");

        Status = ReturnStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AddStatusHistory(ReturnStatus.Completed, "Return completed");
    }

    public void Cancel(string reason)
    {
        if (!CanCancel)
            throw new DomainException("Return cannot be cancelled in current status");

        Status = ReturnStatus.Cancelled;
        InternalNotes = $"Cancelled: {reason}";
        AddStatusHistory(ReturnStatus.Cancelled, reason);
    }

    #endregion

    #region Exchange

    public void ConvertToExchange(Guid newProductId, string newProductName)
    {
        if (Type != ReturnType.Exchange)
            throw new DomainException("Return type must be Exchange");

        foreach (var item in _items)
        {
            item.SetExchangeProduct(newProductId, newProductName);
        }
    }

    #endregion

    #region Private Methods

    private void RecalculateRefundAmount()
    {
        decimal totalRefund = 0;

        foreach (var item in _items)
        {
            // Only include items that passed inspection or haven't been inspected yet
            if (item.InspectionResult == null || item.InspectionResult == InspectionResult.Passed)
            {
                totalRefund += item.UnitPrice.Amount * item.Quantity;
            }
            else if (item.InspectionResult == InspectionResult.PassedWithDeduction)
            {
                // Apply condition-based deduction (e.g., 20% for opened items)
                totalRefund += item.UnitPrice.Amount * item.Quantity * 0.8m;
            }
        }

        // Apply restocking fee if applicable
        if (RestockingFee != null)
        {
            totalRefund -= RestockingFee.Amount;
        }

        RefundAmount = new Money(Math.Max(0, totalRefund), "KWD");
    }

    public void SetRestockingFee(Money fee)
    {
        RestockingFee = fee;
        RecalculateRefundAmount();
    }

    private void AddStatusHistory(ReturnStatus status, string notes)
    {
        _statusHistory.Add(new ReturnStatusHistory(Guid.NewGuid(), status, notes));
    }

    private static string GenerateReturnNumber()
    {
        return $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    #endregion
}

public class ReturnItem : Entity<Guid>
{
    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public ReturnReason Reason { get; private set; }
    public string? ReasonDetails { get; private set; }
    public InspectionResult? InspectionResult { get; private set; }
    public string? InspectionNotes { get; private set; }
    public Guid? ExchangeProductId { get; private set; }
    public string? ExchangeProductName { get; private set; }
    public bool RequiresInspection => Reason != ReturnReason.WrongItemShipped;

    internal ReturnItem(
        Guid id,
        Guid orderLineId,
        Guid productId,
        string productName,
        int quantity,
        Money unitPrice,
        ReturnReason reason,
        string? reasonDetails)
    {
        Id = id;
        OrderLineId = orderLineId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Reason = reason;
        ReasonDetails = reasonDetails;
    }

    private ReturnItem() { } // EF Core

    internal void SetInspectionResult(InspectionResult result, string? notes)
    {
        InspectionResult = result;
        InspectionNotes = notes;
    }

    internal void SetExchangeProduct(Guid productId, string productName)
    {
        ExchangeProductId = productId;
        ExchangeProductName = productName;
    }
}

public class ReturnImage : Entity<Guid>
{
    public string ImageUrl { get; private set; }
    public string? Description { get; private set; }
    public DateTime UploadedAt { get; private set; }

    internal ReturnImage(Guid id, string imageUrl, string? description)
    {
        Id = id;
        ImageUrl = imageUrl;
        Description = description;
        UploadedAt = DateTime.UtcNow;
    }

    private ReturnImage() { } // EF Core
}

public class ReturnStatusHistory : Entity<Guid>
{
    public ReturnStatus Status { get; private set; }
    public string Notes { get; private set; }
    public DateTime Timestamp { get; private set; }

    internal ReturnStatusHistory(Guid id, ReturnStatus status, string notes)
    {
        Id = id;
        Status = status;
        Notes = notes;
        Timestamp = DateTime.UtcNow;
    }

    private ReturnStatusHistory() { } // EF Core
}

public enum ReturnStatus
{
    Pending = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    LabelGenerated = 5,
    InTransit = 6,
    Received = 7,
    Inspecting = 8,
    InspectionPassed = 9,
    PartiallyApproved = 10,
    RefundProcessed = 11,
    Completed = 12,
    Cancelled = 13
}

public enum ReturnType
{
    Refund = 1,
    Exchange = 2,
    StoreCredit = 3
}

public enum ReturnReason
{
    Defective = 1,
    WrongItemShipped = 2,
    NotAsDescribed = 3,
    ChangedMind = 4,
    BetterPriceFound = 5,
    TooLarge = 6,
    TooSmall = 7,
    ArrivedTooLate = 8,
    DamagedInShipping = 9,
    MissingParts = 10
}

public enum InspectionResult
{
    Passed = 1,
    PassedWithDeduction = 2,
    Failed = 3
}

public enum RefundMethod
{
    OriginalPaymentMethod = 1,
    StoreCredit = 2,
    BankTransfer = 3,
    Wallet = 4
}
