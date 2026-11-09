using EShop.Domain.Aggregates.ReturnAggregate;
using EShop.Domain.Common;

namespace EShop.Domain.Events;

public class ReturnRequestCreatedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public ReturnType Type { get; }
    public string Reason { get; }
    public IEnumerable<ReturnItemInfo> Items { get; }

    public ReturnRequestCreatedEvent(Guid returnId, string returnNumber, Guid orderId, Guid customerId, ReturnType type, string reason, IEnumerable<ReturnItemInfo> items)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        OrderId = orderId;
        CustomerId = customerId;
        Type = type;
        Reason = reason;
        Items = items;
    }
}

public class ReturnItemInfo
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }

    public ReturnItemInfo(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

public class ReturnApprovedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public Guid ApprovedBy { get; }
    public decimal EstimatedRefund { get; }

    public ReturnApprovedEvent(Guid returnId, string returnNumber, Guid orderId, Guid customerId, Guid approvedBy, decimal estimatedRefund)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        OrderId = orderId;
        CustomerId = customerId;
        ApprovedBy = approvedBy;
        EstimatedRefund = estimatedRefund;
    }
}

public class ReturnRejectedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public string RejectionReason { get; }

    public ReturnRejectedEvent(Guid returnId, string returnNumber, Guid orderId, Guid customerId, string rejectionReason)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        OrderId = orderId;
        CustomerId = customerId;
        RejectionReason = rejectionReason;
    }
}

public class ReturnLabelGeneratedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public string TrackingNumber { get; }
    public string LabelUrl { get; }

    public ReturnLabelGeneratedEvent(Guid returnId, string returnNumber, string trackingNumber, string labelUrl)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        TrackingNumber = trackingNumber;
        LabelUrl = labelUrl;
    }
}

public class ReturnReceivedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public Guid OrderId { get; }
    public DateTime ReceivedAt { get; }

    public ReturnReceivedEvent(Guid returnId, string returnNumber, Guid orderId, DateTime receivedAt)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        OrderId = orderId;
        ReceivedAt = receivedAt;
    }
}

public class ReturnInspectionCompletedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public bool AllItemsPassed { get; }
    public IEnumerable<InspectionResultInfo> Results { get; }

    public ReturnInspectionCompletedEvent(Guid returnId, string returnNumber, bool allItemsPassed, IEnumerable<InspectionResultInfo> results)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        AllItemsPassed = allItemsPassed;
        Results = results;
    }
}

public class InspectionResultInfo
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public InspectionResult Result { get; }
    public string? Notes { get; }

    public InspectionResultInfo(Guid productId, string productName, InspectionResult result, string? notes)
    {
        ProductId = productId;
        ProductName = productName;
        Result = result;
        Notes = notes;
    }
}

public class ReturnRefundProcessedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal RefundAmount { get; }
    public RefundMethod Method { get; }

    public ReturnRefundProcessedEvent(Guid returnId, string returnNumber, Guid orderId, Guid customerId, decimal refundAmount, RefundMethod method)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        OrderId = orderId;
        CustomerId = customerId;
        RefundAmount = refundAmount;
        Method = method;
    }
}

public class ReturnCompletedEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal FinalRefundAmount { get; }
    public TimeSpan ProcessingTime { get; }

    public ReturnCompletedEvent(Guid returnId, string returnNumber, Guid orderId, Guid customerId, decimal finalRefundAmount, TimeSpan processingTime)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        OrderId = orderId;
        CustomerId = customerId;
        FinalRefundAmount = finalRefundAmount;
        ProcessingTime = processingTime;
    }
}

public class ReturnCancelledEvent : DomainEvent
{
    public Guid ReturnId { get; }
    public string ReturnNumber { get; }
    public Guid CustomerId { get; }
    public string Reason { get; }

    public ReturnCancelledEvent(Guid returnId, string returnNumber, Guid customerId, string reason)
    {
        ReturnId = returnId;
        ReturnNumber = returnNumber;
        CustomerId = customerId;
        Reason = reason;
    }
}
