using EShop.Domain.Aggregates.CustomerAggregate;
using EShop.Domain.Common;

namespace EShop.Domain.Events;

public class CustomerCreatedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string Email { get; }
    public string FullName { get; }
    public Guid? ReferredByCustomerId { get; }

    public CustomerCreatedEvent(Guid customerId, string email, string fullName, Guid? referredByCustomerId)
    {
        CustomerId = customerId;
        Email = email;
        FullName = fullName;
        ReferredByCustomerId = referredByCustomerId;
    }
}

public class CustomerTierUpgradedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public CustomerTier OldTier { get; }
    public CustomerTier NewTier { get; }
    public decimal CurrentYearSpend { get; }

    public CustomerTierUpgradedEvent(Guid customerId, CustomerTier oldTier, CustomerTier newTier, decimal currentYearSpend)
    {
        CustomerId = customerId;
        OldTier = oldTier;
        NewTier = newTier;
        CurrentYearSpend = currentYearSpend;
    }
}

public class LoyaltyPointsEarnedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public int Points { get; }
    public string Description { get; }
    public Guid? OrderId { get; }

    public LoyaltyPointsEarnedEvent(Guid customerId, int points, string description, Guid? orderId)
    {
        CustomerId = customerId;
        Points = points;
        Description = description;
        OrderId = orderId;
    }
}

public class LoyaltyPointsRedeemedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public int Points { get; }
    public decimal ValueRedeemed { get; }
    public Guid? OrderId { get; }

    public LoyaltyPointsRedeemedEvent(Guid customerId, int points, decimal valueRedeemed, Guid? orderId)
    {
        CustomerId = customerId;
        Points = points;
        ValueRedeemed = valueRedeemed;
        OrderId = orderId;
    }
}

public class WalletCreditedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public WalletTransactionType Type { get; }
    public string Description { get; }

    public WalletCreditedEvent(Guid customerId, decimal amount, string currency, WalletTransactionType type, string description)
    {
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Type = type;
        Description = description;
    }
}

public class WalletDebitedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public WalletTransactionType Type { get; }
    public Guid? OrderId { get; }

    public WalletDebitedEvent(Guid customerId, decimal amount, string currency, WalletTransactionType type, Guid? orderId)
    {
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Type = type;
        OrderId = orderId;
    }
}

public class CustomerDeactivatedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string Reason { get; }

    public CustomerDeactivatedEvent(Guid customerId, string reason)
    {
        CustomerId = customerId;
        Reason = reason;
    }
}

public class ReferralBonusAwardedEvent : DomainEvent
{
    public Guid ReferrerId { get; }
    public Guid ReferredCustomerId { get; }
    public decimal BonusAmount { get; }
    public int BonusPoints { get; }

    public ReferralBonusAwardedEvent(Guid referrerId, Guid referredCustomerId, decimal bonusAmount, int bonusPoints)
    {
        ReferrerId = referrerId;
        ReferredCustomerId = referredCustomerId;
        BonusAmount = bonusAmount;
        BonusPoints = bonusPoints;
    }
}
