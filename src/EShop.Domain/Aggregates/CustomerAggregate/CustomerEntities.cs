using EShop.Domain.Common;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.CustomerAggregate;

/// <summary>
/// Customer Address Entity
/// </summary>
public class CustomerAddress : Entity<Guid>
{
    public string Label { get; private set; }
    public Address Address { get; private set; }
    public bool IsDefault { get; private set; }

    internal CustomerAddress(Guid id, string label, Address address, bool isDefault)
    {
        Id = id;
        Label = label;
        Address = address;
        IsDefault = isDefault;
    }

    private CustomerAddress() { } // EF Core

    internal void SetAsDefault() => IsDefault = true;
    internal void SetAsNonDefault() => IsDefault = false;
}

/// <summary>
/// Payment Method Entity
/// </summary>
public class PaymentMethod : Entity<Guid>
{
    public PaymentMethodType Type { get; private set; }
    public string LastFourDigits { get; private set; }
    public string? CardBrand { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    internal PaymentMethod(
        Guid id,
        PaymentMethodType type,
        string lastFourDigits,
        string? cardBrand,
        DateTime? expiryDate,
        bool isDefault)
    {
        Id = id;
        Type = type;
        LastFourDigits = lastFourDigits;
        CardBrand = cardBrand;
        ExpiryDate = expiryDate;
        IsDefault = isDefault;
    }

    private PaymentMethod() { } // EF Core

    internal void SetAsDefault() => IsDefault = true;
    internal void SetAsNonDefault() => IsDefault = false;
}

public enum PaymentMethodType
{
    CreditCard = 1,
    DebitCard = 2,
    BankTransfer = 3,
    Wallet = 4,
    CashOnDelivery = 5,
    Knet = 6,
    ApplePay = 7,
    GooglePay = 8
}

/// <summary>
/// Wallet Transaction
/// </summary>
public class CustomerWalletTransaction : Entity<Guid>
{
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public WalletTransactionType Type { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal CustomerWalletTransaction(
        Guid id,
        decimal amount,
        string description,
        WalletTransactionType type,
        Guid? referenceId)
    {
        Id = id;
        Amount = amount;
        Description = description;
        Type = type;
        ReferenceId = referenceId;
        CreatedAt = DateTime.UtcNow;
    }

    private CustomerWalletTransaction() { } // EF Core
}

public enum WalletTransactionType
{
    TopUp = 1,
    Purchase = 2,
    Refund = 3,
    ReferralBonus = 4,
    PromotionalCredit = 5,
    Cashback = 6,
    Withdrawal = 7
}

/// <summary>
/// Loyalty Points Transaction
/// </summary>
public class LoyaltyPointTransaction : Entity<Guid>
{
    public int Points { get; private set; }
    public string Description { get; private set; }
    public LoyaltyTransactionType Type { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    internal LoyaltyPointTransaction(
        Guid id,
        int points,
        string description,
        LoyaltyTransactionType type,
        Guid? orderId,
        DateTime? expiresAt)
    {
        Id = id;
        Points = points;
        Description = description;
        Type = type;
        OrderId = orderId;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    private LoyaltyPointTransaction() { } // EF Core
}

public enum LoyaltyTransactionType
{
    Purchase = 1,
    Redemption = 2,
    Bonus = 3,
    Expiration = 4,
    Adjustment = 5
}
