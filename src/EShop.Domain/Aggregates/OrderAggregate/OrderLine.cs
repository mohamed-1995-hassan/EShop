using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Entity within the Order aggregate
/// Can only be accessed through the Order (aggregate root)
/// </summary>
public class OrderLine : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Money LineTotal { get; private set; }
    
    private OrderLine() { } // For EF Core
    
    internal OrderLine(Guid productId, string productName, Money unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required");
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        CalculateLineTotal();
    }
    
    internal void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        
        Quantity += quantity;
        CalculateLineTotal();
    }
    
    internal void DecreaseQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
        
        if (Quantity - quantity < 1)
            throw new DomainException("Cannot decrease quantity below 1");
        
        Quantity -= quantity;
        CalculateLineTotal();
    }
    
    private void CalculateLineTotal()
    {
        LineTotal = UnitPrice.Multiply(Quantity);
    }
}
