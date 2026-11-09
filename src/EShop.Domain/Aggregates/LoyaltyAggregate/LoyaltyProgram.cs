using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.LoyaltyAggregate;

/// <summary>
/// Loyalty Program Aggregate - Points and rewards system
/// 
/// BUSINESS RULES:
/// - Points earned on purchases (configurable rate)
/// - Points expire after configurable period
/// - Tier-based multipliers (Bronze, Silver, Gold, Platinum)
/// - Points can be redeemed for discounts or rewards
/// - Bonus points for special events/promotions
/// - Point transfers between members (optional)
/// </summary>
public class LoyaltyAccount : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public string MembershipNumber { get; private set; } = string.Empty;
    public LoyaltyTier Tier { get; private set; }
    public int CurrentPoints { get; private set; }
    public int LifetimePoints { get; private set; }
    public int RedeemedPoints { get; private set; }
    public int ExpiredPoints { get; private set; }
    public decimal LifetimeSpend { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? TierExpiresAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<PointTransaction> _transactions = new();
    public IReadOnlyCollection<PointTransaction> Transactions => _transactions.AsReadOnly();

    private readonly List<LoyaltyReward> _availableRewards = new();
    public IReadOnlyCollection<LoyaltyReward> AvailableRewards => _availableRewards.AsReadOnly();

    public decimal TierMultiplier => Tier switch
    {
        LoyaltyTier.Bronze => 1.0m,
        LoyaltyTier.Silver => 1.25m,
        LoyaltyTier.Gold => 1.5m,
        LoyaltyTier.Platinum => 2.0m,
        LoyaltyTier.Diamond => 3.0m,
        _ => 1.0m
    };

    private LoyaltyAccount() { }

    public static LoyaltyAccount Enroll(Guid customerId, int welcomeBonus = 100)
    {
        var account = new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            MembershipNumber = GenerateMembershipNumber(),
            Tier = LoyaltyTier.Bronze,
            CurrentPoints = welcomeBonus,
            LifetimePoints = welcomeBonus,
            RedeemedPoints = 0,
            ExpiredPoints = 0,
            LifetimeSpend = 0,
            EnrolledAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        if (welcomeBonus > 0)
        {
            account._transactions.Add(PointTransaction.Earn(
                welcomeBonus,
                PointTransactionType.WelcomeBonus,
                "Welcome bonus on enrollment",
                DateTime.UtcNow.AddYears(1)));
        }

        return account;
    }

    #region Point Earning

    public void EarnPoints(Guid orderId, decimal orderAmount, string currency, decimal pointsPerCurrency = 1)
    {
        if (!IsActive)
            throw new DomainException("Loyalty account is not active");

        var basePoints = (int)(orderAmount * pointsPerCurrency);
        var multipliedPoints = (int)(basePoints * TierMultiplier);

        CurrentPoints += multipliedPoints;
        LifetimePoints += multipliedPoints;
        LifetimeSpend += orderAmount;
        LastActivityAt = DateTime.UtcNow;

        _transactions.Add(PointTransaction.Earn(
            multipliedPoints,
            PointTransactionType.Purchase,
            $"Order purchase - Base: {basePoints}, Multiplier: {TierMultiplier}x",
            DateTime.UtcNow.AddYears(2),
            orderId));

        UpdateTier();
    }

    public void EarnBonusPoints(int points, string reason, PointTransactionType type = PointTransactionType.Bonus)
    {
        if (!IsActive)
            throw new DomainException("Loyalty account is not active");

        if (points <= 0)
            throw new DomainException("Points must be positive");

        CurrentPoints += points;
        LifetimePoints += points;
        LastActivityAt = DateTime.UtcNow;

        _transactions.Add(PointTransaction.Earn(
            points,
            type,
            reason,
            DateTime.UtcNow.AddYears(1)));
    }

    public void EarnReferralBonus(Guid referredCustomerId, int points = 500)
    {
        EarnBonusPoints(points, $"Referral bonus for customer {referredCustomerId}", PointTransactionType.Referral);
    }

    public void EarnBirthdayBonus(int points = 200)
    {
        EarnBonusPoints(points, "Birthday bonus", PointTransactionType.Birthday);
    }

    public void EarnReviewBonus(Guid reviewId, int points = 50)
    {
        EarnBonusPoints(points, $"Review bonus for review {reviewId}", PointTransactionType.Review);
    }

    #endregion

    #region Point Redemption

    public PointRedemption RedeemForDiscount(int points, Money discountValue, Guid orderId)
    {
        if (!IsActive)
            throw new DomainException("Loyalty account is not active");

        if (points > CurrentPoints)
            throw new DomainException($"Insufficient points. Available: {CurrentPoints}, Requested: {points}");

        if (points <= 0)
            throw new DomainException("Points must be positive");

        CurrentPoints -= points;
        RedeemedPoints += points;
        LastActivityAt = DateTime.UtcNow;

        var transaction = PointTransaction.Redeem(
            points,
            PointTransactionType.Redemption,
            $"Discount of {discountValue.Amount} {discountValue.Currency}",
            orderId);

        _transactions.Add(transaction);

        return new PointRedemption(
            Guid.NewGuid(),
            points,
            discountValue,
            RedemptionType.Discount,
            orderId);
    }

    public PointRedemption RedeemForReward(Guid rewardId, string rewardName, int requiredPoints)
    {
        if (!IsActive)
            throw new DomainException("Loyalty account is not active");

        if (requiredPoints > CurrentPoints)
            throw new DomainException($"Insufficient points for reward. Required: {requiredPoints}, Available: {CurrentPoints}");

        CurrentPoints -= requiredPoints;
        RedeemedPoints += requiredPoints;
        LastActivityAt = DateTime.UtcNow;

        _transactions.Add(PointTransaction.Redeem(
            requiredPoints,
            PointTransactionType.RewardRedemption,
            $"Reward: {rewardName}"));

        return new PointRedemption(
            Guid.NewGuid(),
            requiredPoints,
            Money.Zero("KWD"),
            RedemptionType.Reward,
            rewardId: rewardId);
    }

    public void ReverseRedemption(int points, Guid originalTransactionId, string reason)
    {
        CurrentPoints += points;
        RedeemedPoints -= points;
        LastActivityAt = DateTime.UtcNow;

        _transactions.Add(PointTransaction.Earn(
            points,
            PointTransactionType.RedemptionReversal,
            $"Reversal: {reason}",
            DateTime.UtcNow.AddYears(1),
            originalTransactionId));
    }

    #endregion

    #region Point Management

    public void ExpirePoints(DateTime asOf)
    {
        var expiredTransactions = _transactions
            .Where(t => t.ExpiresAt.HasValue && 
                        t.ExpiresAt.Value <= asOf && 
                        t.Type != PointTransactionType.Redemption &&
                        !t.IsExpired)
            .ToList();

        foreach (var transaction in expiredTransactions)
        {
            var remainingPoints = transaction.RemainingPoints;
            if (remainingPoints > 0)
            {
                transaction.MarkAsExpired();
                CurrentPoints -= remainingPoints;
                ExpiredPoints += remainingPoints;

                _transactions.Add(PointTransaction.Expire(
                    remainingPoints,
                    $"Points expired from transaction {transaction.Id}"));
            }
        }
    }

    public void TransferPoints(LoyaltyAccount recipient, int points, string reason)
    {
        if (!IsActive || !recipient.IsActive)
            throw new DomainException("Both accounts must be active for transfer");

        if (points > CurrentPoints)
            throw new DomainException("Insufficient points for transfer");

        if (points <= 0)
            throw new DomainException("Transfer amount must be positive");

        // Deduct from sender
        CurrentPoints -= points;
        _transactions.Add(PointTransaction.Redeem(
            points,
            PointTransactionType.TransferOut,
            $"Transfer to {recipient.MembershipNumber}: {reason}"));

        // Add to recipient
        recipient.CurrentPoints += points;
        recipient.LifetimePoints += points;
        recipient._transactions.Add(PointTransaction.Earn(
            points,
            PointTransactionType.TransferIn,
            $"Transfer from {MembershipNumber}: {reason}",
            DateTime.UtcNow.AddYears(1)));
    }

    public void AdjustPoints(int pointsDelta, string reason, Guid adjustedBy)
    {
        if (pointsDelta == 0)
            throw new DomainException("Adjustment amount cannot be zero");

        if (pointsDelta < 0 && Math.Abs(pointsDelta) > CurrentPoints)
            throw new DomainException("Cannot adjust below zero points");

        CurrentPoints += pointsDelta;
        if (pointsDelta > 0)
            LifetimePoints += pointsDelta;

        var type = pointsDelta > 0 ? PointTransactionType.Adjustment : PointTransactionType.Deduction;
        _transactions.Add(pointsDelta > 0
            ? PointTransaction.Earn(pointsDelta, type, $"Manual adjustment: {reason}", DateTime.UtcNow.AddYears(1))
            : PointTransaction.Redeem(Math.Abs(pointsDelta), type, $"Manual deduction: {reason}"));
    }

    #endregion

    #region Tier Management

    private void UpdateTier()
    {
        var newTier = LifetimeSpend switch
        {
            >= 10000 => LoyaltyTier.Diamond,
            >= 5000 => LoyaltyTier.Platinum,
            >= 2000 => LoyaltyTier.Gold,
            >= 500 => LoyaltyTier.Silver,
            _ => LoyaltyTier.Bronze
        };

        if (newTier > Tier)
        {
            Tier = newTier;
            TierExpiresAt = DateTime.UtcNow.AddYears(1);
            
            // Tier upgrade bonus
            var bonusPoints = newTier switch
            {
                LoyaltyTier.Silver => 100,
                LoyaltyTier.Gold => 250,
                LoyaltyTier.Platinum => 500,
                LoyaltyTier.Diamond => 1000,
                _ => 0
            };

            if (bonusPoints > 0)
            {
                EarnBonusPoints(bonusPoints, $"Tier upgrade bonus - Welcome to {newTier}!", PointTransactionType.TierBonus);
            }
        }
    }

    public void EvaluateTierStatus()
    {
        if (TierExpiresAt.HasValue && DateTime.UtcNow > TierExpiresAt.Value)
        {
            // Check if customer maintained tier through recent spend
            var recentSpend = _transactions
                .Where(t => t.CreatedAt >= DateTime.UtcNow.AddYears(-1) && 
                            t.Type == PointTransactionType.Purchase)
                .Sum(t => t.Points / TierMultiplier);

            var maintainedTier = recentSpend switch
            {
                >= 10000 => LoyaltyTier.Diamond,
                >= 5000 => LoyaltyTier.Platinum,
                >= 2000 => LoyaltyTier.Gold,
                >= 500 => LoyaltyTier.Silver,
                _ => LoyaltyTier.Bronze
            };

            if (maintainedTier < Tier)
            {
                Tier = maintainedTier;
            }

            TierExpiresAt = DateTime.UtcNow.AddYears(1);
        }
    }

    #endregion

    #region Account Management

    public void Suspend(string reason)
    {
        IsActive = false;
        _transactions.Add(PointTransaction.Redeem(
            0,
            PointTransactionType.AccountSuspension,
            $"Account suspended: {reason}"));
    }

    public void Reactivate()
    {
        IsActive = true;
        LastActivityAt = DateTime.UtcNow;
    }

    public LoyaltyAccountSummary GetSummary()
    {
        return new LoyaltyAccountSummary(
            MembershipNumber,
            Tier,
            CurrentPoints,
            LifetimePoints,
            RedeemedPoints,
            LifetimeSpend,
            TierMultiplier,
            CalculatePointsToNextTier(),
            GetExpiringPoints(30));
    }

    private int CalculatePointsToNextTier()
    {
        var nextTierThreshold = Tier switch
        {
            LoyaltyTier.Bronze => 500,
            LoyaltyTier.Silver => 2000,
            LoyaltyTier.Gold => 5000,
            LoyaltyTier.Platinum => 10000,
            _ => 0
        };

        return nextTierThreshold > 0 ? (int)(nextTierThreshold - LifetimeSpend) : 0;
    }

    private int GetExpiringPoints(int withinDays)
    {
        var expiryDate = DateTime.UtcNow.AddDays(withinDays);
        return _transactions
            .Where(t => t.ExpiresAt.HasValue && 
                        t.ExpiresAt.Value <= expiryDate && 
                        !t.IsExpired &&
                        t.RemainingPoints > 0)
            .Sum(t => t.RemainingPoints);
    }

    #endregion

    private static string GenerateMembershipNumber()
    {
        return $"LYL{DateTime.UtcNow:yyMMdd}{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

public class PointTransaction : Entity<Guid>
{
    public int Points { get; private set; }
    public int RemainingPoints { get; private set; }
    public PointTransactionType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsExpired { get; private set; }
    public Guid? ReferenceId { get; private set; }

    private PointTransaction() { }

    internal static PointTransaction Earn(
        int points, 
        PointTransactionType type, 
        string description, 
        DateTime? expiresAt = null,
        Guid? referenceId = null)
    {
        return new PointTransaction
        {
            Id = Guid.NewGuid(),
            Points = points,
            RemainingPoints = points,
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsExpired = false,
            ReferenceId = referenceId
        };
    }

    internal static PointTransaction Redeem(
        int points, 
        PointTransactionType type, 
        string description,
        Guid? referenceId = null)
    {
        return new PointTransaction
        {
            Id = Guid.NewGuid(),
            Points = -points,
            RemainingPoints = 0,
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null,
            IsExpired = false,
            ReferenceId = referenceId
        };
    }

    internal static PointTransaction Expire(int points, string description)
    {
        return new PointTransaction
        {
            Id = Guid.NewGuid(),
            Points = -points,
            RemainingPoints = 0,
            Type = PointTransactionType.Expiration,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            IsExpired = true
        };
    }

    internal void MarkAsExpired()
    {
        IsExpired = true;
        RemainingPoints = 0;
    }

    internal void UsePoints(int usedPoints)
    {
        if (usedPoints > RemainingPoints)
            throw new DomainException("Cannot use more points than remaining");
        RemainingPoints -= usedPoints;
    }
}

public class PointRedemption
{
    public Guid Id { get; }
    public int PointsUsed { get; }
    public Money DiscountValue { get; }
    public RedemptionType Type { get; }
    public Guid? OrderId { get; }
    public Guid? RewardId { get; }
    public DateTime RedeemedAt { get; }

    public PointRedemption(
        Guid id, 
        int pointsUsed, 
        Money discountValue, 
        RedemptionType type,
        Guid? orderId = null,
        Guid? rewardId = null)
    {
        Id = id;
        PointsUsed = pointsUsed;
        DiscountValue = discountValue;
        Type = type;
        OrderId = orderId;
        RewardId = rewardId;
        RedeemedAt = DateTime.UtcNow;
    }
}

public class LoyaltyReward : Entity<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int PointsCost { get; private set; }
    public RewardType Type { get; private set; }
    public string? ImageUrl { get; private set; }
    public int QuantityAvailable { get; private set; }
    public LoyaltyTier MinimumTier { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }

    internal LoyaltyReward(
        Guid id,
        string name,
        string description,
        int pointsCost,
        RewardType type,
        LoyaltyTier minimumTier = LoyaltyTier.Bronze)
    {
        Id = id;
        Name = name;
        Description = description;
        PointsCost = pointsCost;
        Type = type;
        MinimumTier = minimumTier;
        QuantityAvailable = -1; // Unlimited
        IsActive = true;
    }

    private LoyaltyReward() { }
}

public record LoyaltyAccountSummary(
    string MembershipNumber,
    LoyaltyTier Tier,
    int CurrentPoints,
    int LifetimePoints,
    int RedeemedPoints,
    decimal LifetimeSpend,
    decimal TierMultiplier,
    int PointsToNextTier,
    int ExpiringPointsNext30Days);

public enum LoyaltyTier
{
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Diamond = 5
}

public enum PointTransactionType
{
    Purchase = 1,
    WelcomeBonus = 2,
    Referral = 3,
    Birthday = 4,
    Review = 5,
    Bonus = 6,
    TierBonus = 7,
    Redemption = 8,
    RewardRedemption = 9,
    RedemptionReversal = 10,
    TransferIn = 11,
    TransferOut = 12,
    Expiration = 13,
    Adjustment = 14,
    Deduction = 15,
    AccountSuspension = 16
}

public enum RedemptionType
{
    Discount = 1,
    Reward = 2,
    GiftCard = 3,
    FreeShipping = 4,
    Experience = 5
}

public enum RewardType
{
    PhysicalProduct = 1,
    DigitalProduct = 2,
    Discount = 3,
    FreeShipping = 4,
    Experience = 5,
    Upgrade = 6
}
