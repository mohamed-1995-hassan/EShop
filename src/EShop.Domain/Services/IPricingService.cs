using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Entities;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Services;

/// <summary>
/// Domain Service - logic that doesn't belong to a single entity
/// </summary>
public interface IPricingService
{
    Money CalculateDiscount(Order order, Customer customer);
    Money CalculateShippingCost(Order order, Address destination);
}

public class PricingService : IPricingService
{
    public Money CalculateDiscount(Order order, Customer customer)
    {
        var discount = Money.Zero(order.TotalAmount.Currency);
        
        // Business rule: 10% discount for orders over 100
        if (order.TotalAmount.Amount > 100)
        {
            discount = order.TotalAmount.Multiply(0.10m);
        }
        
        return discount;
    }
    
    public Money CalculateShippingCost(Order order, Address destination)
    {
        // Business rule: Free shipping for orders over 50
        if (order.TotalAmount.Amount >= 50)
        {
            return Money.Zero(order.TotalAmount.Currency);
        }
        
        // Fixed shipping cost
        return new Money(5, order.TotalAmount.Currency);
    }
}
