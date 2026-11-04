using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.ValueObjects;

/// <summary>
/// Value Object representing monetary value
/// Immutable - once created, cannot be changed
/// </summary>
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    private Money() { } // For EF Core
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative");
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required");
        
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
    
    // Factory methods
    public static Money Zero(string currency) => new(0, currency);
    public static Money FromKWD(decimal amount) => new(amount, "KWD");
    public static Money FromUSD(decimal amount) => new(amount, "USD");
    
    // Operations - return NEW Money objects (immutability)
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }
    
    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (Amount < other.Amount)
            throw new DomainException("Cannot subtract: would result in negative amount");
        
        return new Money(Amount - other.Amount, Currency);
    }
    
    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }
    
    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot operate on different currencies: {Currency} and {other.Currency}");
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
    public override string ToString() => $"{Amount:N2} {Currency}";
}
