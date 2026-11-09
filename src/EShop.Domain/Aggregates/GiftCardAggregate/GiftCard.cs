using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.GiftCardAggregate;

/// <summary>
/// Gift Card Aggregate - Prepaid store credit cards
/// 
/// BUSINESS RULES:
/// - Gift cards have fixed denominations or custom amounts
/// - Cannot exceed $500 per card
/// - Expiry typically 1-3 years
/// - Partial redemption allowed
/// - Can be physical or digital
/// - Reloadable option for store cards
/// - Balance cannot go negative
/// </summary>
public class GiftCard : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string? Pin { get; private set; }
    public GiftCardType Type { get; private set; }
    public GiftCardStatus Status { get; private set; }
    public Money InitialBalance { get; private set; } = null!;
    public Money CurrentBalance { get; private set; } = null!;
    public bool IsReloadable { get; private set; }
    public decimal? MaxBalance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public Guid? PurchasedByCustomerId { get; private set; }
    public Guid? RecipientCustomerId { get; private set; }
    public string? RecipientEmail { get; private set; }
    public string? RecipientName { get; private set; }
    public string? PersonalMessage { get; private set; }
    public DateTime? ScheduledDeliveryDate { get; private set; }
    public bool IsDelivered { get; private set; }

    private readonly List<GiftCardTransaction> _transactions = new();
    public IReadOnlyCollection<GiftCardTransaction> Transactions => _transactions.AsReadOnly();

    public bool IsActive => Status == GiftCardStatus.Active;
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool HasBalance => CurrentBalance.Amount > 0;
    public bool CanRedeem => IsActive && HasBalance && !IsExpired;

    private GiftCard() { }

    public static GiftCard CreateDigital(
        decimal amount,
        string currency,
        Guid purchasedByCustomerId,
        string recipientEmail,
        string recipientName,
        string? personalMessage = null,
        DateTime? scheduledDelivery = null,
        int expiryYears = 2)
    {
        ValidateAmount(amount);

        var giftCard = CreateBase(amount, currency, GiftCardType.Digital, expiryYears);
        giftCard.PurchasedByCustomerId = purchasedByCustomerId;
        giftCard.RecipientEmail = recipientEmail;
        giftCard.RecipientName = recipientName;
        giftCard.PersonalMessage = personalMessage;
        giftCard.ScheduledDeliveryDate = scheduledDelivery;
        
        return giftCard;
    }

    public static GiftCard CreatePhysical(
        decimal amount,
        string currency,
        Guid? purchasedByCustomerId = null,
        int expiryYears = 3)
    {
        ValidateAmount(amount);

        var giftCard = CreateBase(amount, currency, GiftCardType.Physical, expiryYears);
        giftCard.PurchasedByCustomerId = purchasedByCustomerId;
        giftCard.Pin = GeneratePin();
        
        return giftCard;
    }

    public static GiftCard CreateStoreCredit(
        decimal amount,
        string currency,
        Guid customerId,
        string reason,
        bool isReloadable = true)
    {
        var giftCard = CreateBase(amount, currency, GiftCardType.StoreCredit, 5);
        giftCard.RecipientCustomerId = customerId;
        giftCard.IsReloadable = isReloadable;
        giftCard.MaxBalance = 1000; // Store credit can hold more
        giftCard.Status = GiftCardStatus.Active;
        giftCard.ActivatedAt = DateTime.UtcNow;
        
        giftCard._transactions.Add(GiftCardTransaction.Credit(
            giftCard.InitialBalance,
            $"Store credit issued: {reason}",
            GiftCardTransactionType.Issue));

        return giftCard;
    }

    public static GiftCard CreatePromoCard(
        decimal amount,
        string currency,
        string campaignName,
        DateTime expiresAt)
    {
        var giftCard = CreateBase(amount, currency, GiftCardType.Promotional, 0);
        giftCard.ExpiresAt = expiresAt;
        giftCard.PersonalMessage = $"Promotional gift card - {campaignName}";
        giftCard.Status = GiftCardStatus.Active;
        giftCard.ActivatedAt = DateTime.UtcNow;

        giftCard._transactions.Add(GiftCardTransaction.Credit(
            giftCard.InitialBalance,
            $"Promotional card: {campaignName}",
            GiftCardTransactionType.Issue));

        return giftCard;
    }

    private static GiftCard CreateBase(decimal amount, string currency, GiftCardType type, int expiryYears)
    {
        var balance = new Money(amount, currency);

        return new GiftCard
        {
            Id = Guid.NewGuid(),
            Code = GenerateCode(),
            Type = type,
            Status = GiftCardStatus.Pending,
            InitialBalance = balance,
            CurrentBalance = balance,
            IsReloadable = false,
            MaxBalance = 500,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiryYears > 0 ? DateTime.UtcNow.AddYears(expiryYears) : null,
            IsDelivered = false
        };
    }

    #region Activation & Delivery

    public void Activate()
    {
        if (Status != GiftCardStatus.Pending)
            throw new DomainException("Gift card cannot be activated in current status");

        Status = GiftCardStatus.Active;
        ActivatedAt = DateTime.UtcNow;

        _transactions.Add(GiftCardTransaction.Credit(
            InitialBalance,
            "Gift card activated",
            GiftCardTransactionType.Issue));
    }

    public void MarkAsDelivered()
    {
        if (Type != GiftCardType.Digital)
            throw new DomainException("Only digital gift cards track delivery");

        IsDelivered = true;
    }

    public void AssignToRecipient(Guid customerId)
    {
        RecipientCustomerId = customerId;
    }

    #endregion

    #region Redemption

    public Money Redeem(Money amount, Guid orderId, string orderNumber)
    {
        if (!CanRedeem)
            throw new DomainException("Gift card cannot be redeemed");

        if (amount.Currency != CurrentBalance.Currency)
            throw new DomainException("Currency mismatch");

        var redeemAmount = amount.Amount > CurrentBalance.Amount 
            ? CurrentBalance 
            : amount;

        CurrentBalance = CurrentBalance.Subtract(redeemAmount);

        _transactions.Add(GiftCardTransaction.Debit(
            redeemAmount,
            $"Redeemed for order {orderNumber}",
            GiftCardTransactionType.Redemption,
            orderId));

        if (CurrentBalance.Amount == 0 && !IsReloadable)
        {
            Status = GiftCardStatus.Depleted;
        }

        return redeemAmount;
    }

    public void ReverseRedemption(Guid orderId, Money amount, string reason)
    {
        if (!IsActive && Status != GiftCardStatus.Depleted)
            throw new DomainException("Cannot reverse on inactive gift card");

        CurrentBalance = CurrentBalance.Add(amount);

        if (Status == GiftCardStatus.Depleted)
            Status = GiftCardStatus.Active;

        _transactions.Add(GiftCardTransaction.Credit(
            amount,
            $"Reversal: {reason}",
            GiftCardTransactionType.Reversal,
            orderId));
    }

    #endregion

    #region Reload

    public void Reload(Money amount, Guid? transactionId = null)
    {
        if (!IsReloadable)
            throw new DomainException("Gift card is not reloadable");

        if (!IsActive)
            throw new DomainException("Gift card is not active");

        var newBalance = CurrentBalance.Amount + amount.Amount;
        if (MaxBalance.HasValue && newBalance > MaxBalance.Value)
            throw new DomainException($"Reload would exceed maximum balance of {MaxBalance.Value}");

        CurrentBalance = CurrentBalance.Add(amount);

        _transactions.Add(GiftCardTransaction.Credit(
            amount,
            "Card reloaded",
            GiftCardTransactionType.Reload,
            transactionId));
    }

    #endregion

    #region Management

    public void Suspend(string reason)
    {
        if (!IsActive)
            throw new DomainException("Only active gift cards can be suspended");

        Status = GiftCardStatus.Suspended;

        _transactions.Add(GiftCardTransaction.Status(
            CurrentBalance,
            $"Suspended: {reason}"));
    }

    public void Reactivate()
    {
        if (Status != GiftCardStatus.Suspended)
            throw new DomainException("Only suspended gift cards can be reactivated");

        if (IsExpired)
            throw new DomainException("Cannot reactivate expired gift card");

        Status = GiftCardStatus.Active;

        _transactions.Add(GiftCardTransaction.Status(
            CurrentBalance,
            "Reactivated"));
    }

    public void Cancel(string reason)
    {
        if (Status == GiftCardStatus.Cancelled)
            throw new DomainException("Gift card already cancelled");

        Status = GiftCardStatus.Cancelled;

        _transactions.Add(GiftCardTransaction.Status(
            CurrentBalance,
            $"Cancelled: {reason}"));
    }

    public void ExtendExpiry(DateTime newExpiryDate)
    {
        if (newExpiryDate <= DateTime.UtcNow)
            throw new DomainException("New expiry date must be in the future");

        ExpiresAt = newExpiryDate;
    }

    public bool ValidatePin(string pin)
    {
        if (string.IsNullOrEmpty(Pin))
            return true; // No PIN required

        return Pin == pin;
    }

    #endregion

    #region Balance Inquiry

    public GiftCardBalanceInfo GetBalanceInfo()
    {
        return new GiftCardBalanceInfo(
            Code,
            CurrentBalance,
            InitialBalance,
            Status,
            ExpiresAt,
            IsReloadable,
            _transactions.Count);
    }

    #endregion

    #region Private Methods

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero");
        if (amount > 500)
            throw new DomainException("Gift card amount cannot exceed 500");
    }

    private static string GenerateCode()
    {
        // Format: XXXX-XXXX-XXXX-XXXX (16 chars + dashes)
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No confusing chars (0/O, 1/I/L)
        var random = new Random();
        var code = new char[16];
        
        for (int i = 0; i < 16; i++)
            code[i] = chars[random.Next(chars.Length)];

        return $"{new string(code[0..4])}-{new string(code[4..8])}-{new string(code[8..12])}-{new string(code[12..16])}";
    }

    private static string GeneratePin()
    {
        var random = new Random();
        return random.Next(1000, 9999).ToString();
    }

    #endregion
}

public class GiftCardTransaction : Entity<Guid>
{
    public GiftCardTransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public Money BalanceAfter { get; private set; }
    public string Description { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private GiftCardTransaction() { }

    internal static GiftCardTransaction Credit(Money amount, string description, GiftCardTransactionType type, Guid? referenceId = null)
    {
        return new GiftCardTransaction
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            BalanceAfter = amount, // Will be updated by caller
            Description = description,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal static GiftCardTransaction Debit(Money amount, string description, GiftCardTransactionType type, Guid? referenceId = null)
    {
        return new GiftCardTransaction
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = new Money(-amount.Amount, amount.Currency),
            BalanceAfter = Money.Zero(amount.Currency), // Will be updated by caller
            Description = description,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal static GiftCardTransaction Status(Money currentBalance, string description)
    {
        return new GiftCardTransaction
        {
            Id = Guid.NewGuid(),
            Type = GiftCardTransactionType.StatusChange,
            Amount = Money.Zero(currentBalance.Currency),
            BalanceAfter = currentBalance,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public record GiftCardBalanceInfo(
    string Code,
    Money CurrentBalance,
    Money InitialBalance,
    GiftCardStatus Status,
    DateTime? ExpiresAt,
    bool IsReloadable,
    int TransactionCount);

public enum GiftCardType
{
    Digital = 1,
    Physical = 2,
    StoreCredit = 3,
    Promotional = 4,
    Corporate = 5
}

public enum GiftCardStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Depleted = 4,
    Expired = 5,
    Cancelled = 6
}

public enum GiftCardTransactionType
{
    Issue = 1,
    Redemption = 2,
    Reload = 3,
    Reversal = 4,
    Adjustment = 5,
    StatusChange = 6,
    Expiry = 7
}
