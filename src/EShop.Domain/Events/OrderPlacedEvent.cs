using EShop.Domain.Common;

namespace EShop.Domain.Events;

/// <summary>
/// Domain Event - raised when an order is successfully placed.
/// 
/// HANDLERS CAN:
/// - Send confirmation email to customer
/// - Reserve inventory
/// - Update analytics/reporting
/// - Notify warehouse for picking
/// </summary>
public class OrderPlacedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }
    public IReadOnlyList<OrderPlacedItem> Items { get; }
    
    public OrderPlacedEvent(
        Guid orderId, 
        Guid customerId, 
        decimal totalAmount,
        string currency,
        IEnumerable<OrderPlacedItem> items)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Currency = currency;
        Items = items.ToList().AsReadOnly();
    }
}

/// <summary>
/// Item information included in OrderPlacedEvent
/// </summary>
public class OrderPlacedItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    
    public OrderPlacedItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
