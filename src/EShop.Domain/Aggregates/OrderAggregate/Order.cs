using EShop.Domain.Common;
using EShop.Domain.Events;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Order Aggregate Root
/// 
/// KEY DDD CONCEPTS DEMONSTRATED:
/// 1. AGGREGATE ROOT: Entry point to the Order aggregate
/// 2. INVARIANTS: Business rules enforced in methods
/// 3. ENCAPSULATION: Private setters, read-only collections
/// 4. DOMAIN EVENTS: Raised when important things happen
/// 5. FACTORY METHOD: Create() for controlled instantiation
/// 
/// BUSINESS RULES (Invariants):
/// - Cannot add/remove items from non-pending orders
/// - Cannot place an order without items
/// - Cannot ship before placing
/// - Cannot cancel shipped orders
/// </summary>
public class Order : AggregateRoot<Guid>
{
    // Properties with PRIVATE SETTERS - state can only change through methods
    public Guid CustomerId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    
    // ENCAPSULATED COLLECTION - external code cannot modify directly
    private readonly List<OrderLine> _orderLines = new();
    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();
    
    // Private constructor forces use of Factory Method
    private Order() { }
    
    /// <summary>
    /// FACTORY METHOD - Complex creation logic encapsulated
    /// </summary>
    public static Order Create(Guid customerId, Address shippingAddress, string currency = "KWD")
    {
        // GUARD CLAUSES - validate input
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required");
        
        ArgumentNullException.ThrowIfNull(shippingAddress);
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            TotalAmount = Money.Zero(currency)
        };
        
        // Note: No event for creation - order isn't "real" until placed
        return order;
    }
    
    /// <summary>
    /// BEHAVIOR: Add item to order
    /// Demonstrates: Invariant enforcement, Domain Event
    /// </summary>
    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        // INVARIANT: Can only modify pending orders
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot add items to order in {Status} status");
        
        // INVARIANT: Quantity must be positive
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        
        // BUSINESS LOGIC: Consolidate same products
        var existingLine = _orderLines.FirstOrDefault(x => x.ProductId == productId);
        if (existingLine != null)
        {
            existingLine.IncreaseQuantity(quantity);
        }
        else
        {
            var line = new OrderLine(productId, productName, unitPrice, quantity);
            _orderLines.Add(line);
        }
        
        RecalculateTotal();
        
        // DOMAIN EVENT: Item was added
        AddDomainEvent(new OrderItemAddedEvent(
            Id, 
            productId, 
            productName, 
            quantity, 
            unitPrice.Amount));
    }
    
    /// <summary>
    /// BEHAVIOR: Remove item from order
    /// </summary>
    public void RemoveItem(Guid productId)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot remove items from order in {Status} status");
        
        var line = _orderLines.FirstOrDefault(x => x.ProductId == productId)
            ?? throw new DomainException($"Product {productId} not found in order");
        
        _orderLines.Remove(line);
        RecalculateTotal();
    }
    
    /// <summary>
    /// BEHAVIOR: Place the order - CRITICAL BUSINESS OPERATION
    /// Demonstrates: Multiple invariants, Rich domain event
    /// </summary>
    public void Place()
    {
        // INVARIANT: Must be in correct state
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot place order in {Status} status");
        
        // INVARIANT: Must have items
        if (!_orderLines.Any())
            throw new DomainException("Cannot place an order without items");
        
        // INVARIANT: Must have valid total
        if (TotalAmount.Amount <= 0)
            throw new DomainException("Order total must be greater than zero");
        
        // STATE TRANSITION
        Status = OrderStatus.Placed;
        
        // DOMAIN EVENT with full context for handlers
        var items = _orderLines.Select(line => 
            new OrderPlacedItem(
                line.ProductId,
                line.ProductName,
                line.Quantity,
                line.UnitPrice.Amount))
            .ToList();
        
        AddDomainEvent(new OrderPlacedEvent(
            Id, 
            CustomerId, 
            TotalAmount.Amount,
            TotalAmount.Currency,
            items));
    }
    
    /// <summary>
    /// BEHAVIOR: Ship the order
    /// </summary>
    public void Ship(string? trackingNumber = null, string? carrier = null)
    {
        if (Status != OrderStatus.Placed && Status != OrderStatus.Confirmed)
            throw new DomainException("Order must be placed or confirmed before shipping");
        
        Status = OrderStatus.Shipped;
        
        AddDomainEvent(new OrderShippedEvent(Id, trackingNumber, carrier));
    }
    
    /// <summary>
    /// BEHAVIOR: Cancel the order
    /// </summary>
    public void Cancel(string reason = "Customer requested cancellation")
    {
        if (Status == OrderStatus.Shipped)
            throw new DomainException("Cannot cancel a shipped order");
        
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled");
        
        if (Status == OrderStatus.Completed)
            throw new DomainException("Cannot cancel a completed order");
        
        var refundAmount = Status == OrderStatus.Placed ? TotalAmount.Amount : 0;
        
        Status = OrderStatus.Cancelled;
        
        AddDomainEvent(new OrderCancelledEvent(Id, CustomerId, reason, refundAmount));
    }
    
    /// <summary>
    /// BEHAVIOR: Complete the order (delivery confirmed)
    /// </summary>
    public void Complete()
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainException("Order must be shipped before completion");
        
        Status = OrderStatus.Completed;
    }

    /// <summary>
    /// BEHAVIOR: Confirm the order after successful payment
    /// Used by the Order Processing Saga after payment completion.
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Placed)
            throw new DomainException("Order must be placed before confirmation");
        
        Status = OrderStatus.Confirmed;
        
        AddDomainEvent(new OrderConfirmedEvent(Id, CustomerId, TotalAmount.Amount));
    }
    
    /// <summary>
    /// PRIVATE METHOD: Recalculate total - internal consistency
    /// </summary>
    private void RecalculateTotal()
    {
        var currency = TotalAmount.Currency;
        TotalAmount = _orderLines.Aggregate(
            Money.Zero(currency),
            (total, line) => total.Add(line.LineTotal));
    }
}

/// <summary>
/// Order Status - State machine for order lifecycle
/// </summary>
public enum OrderStatus
{
    Pending = 0,    // Order created, items being added
    Placed = 1,     // Order confirmed by customer
    Confirmed = 2,  // Payment confirmed, ready for fulfillment
    Shipped = 3,    // Order shipped
    Completed = 4,  // Delivery confirmed
    Cancelled = 5   // Order cancelled
}
