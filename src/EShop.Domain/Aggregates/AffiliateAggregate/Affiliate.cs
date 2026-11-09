using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.AffiliateAggregate;

/// <summary>
/// Affiliate/Referral Program Aggregate
/// 
/// BUSINESS RULES:
/// - Affiliates earn commission on referred sales
/// - Tiered commission rates based on performance
/// - Cookie tracking duration for attribution
/// - Minimum payout thresholds
/// - Fraud detection and validation
/// </summary>
public class Affiliate : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string AffiliateCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Website { get; private set; }
    public string? PaypalEmail { get; private set; }
    public string? BankAccountInfo { get; private set; }
    public AffiliateStatus Status { get; private set; }
    public AffiliateTier Tier { get; private set; }
    public decimal CommissionRate { get; private set; }
    public int CookieDurationDays { get; private set; }
    public Money TotalEarnings { get; private set; } = null!;
    public Money PendingEarnings { get; private set; } = null!;
    public Money PaidEarnings { get; private set; } = null!;
    public Money MinimumPayoutThreshold { get; private set; } = null!;
    public int TotalReferrals { get; private set; }
    public int TotalConversions { get; private set; }
    public decimal ConversionRate => TotalReferrals > 0 ? (decimal)TotalConversions / TotalReferrals * 100 : 0;
    public DateTime AppliedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? LastPayoutAt { get; private set; }

    private readonly List<Referral> _referrals = new();
    public IReadOnlyCollection<Referral> Referrals => _referrals.AsReadOnly();

    private readonly List<Commission> _commissions = new();
    public IReadOnlyCollection<Commission> Commissions => _commissions.AsReadOnly();

    private readonly List<Payout> _payouts = new();
    public IReadOnlyCollection<Payout> Payouts => _payouts.AsReadOnly();

    private Affiliate() { }

    public static Affiliate Apply(
        Guid userId,
        string name,
        string email,
        string? website = null,
        string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required");

        return new Affiliate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AffiliateCode = GenerateAffiliateCode(),
            Name = name,
            Email = email,
            Website = website,
            Status = AffiliateStatus.Pending,
            Tier = AffiliateTier.Bronze,
            CommissionRate = 5m, // 5% default
            CookieDurationDays = 30,
            TotalEarnings = Money.Zero(currency),
            PendingEarnings = Money.Zero(currency),
            PaidEarnings = Money.Zero(currency),
            MinimumPayoutThreshold = new Money(50, currency),
            TotalReferrals = 0,
            TotalConversions = 0,
            AppliedAt = DateTime.UtcNow
        };
    }

    #region Application Management

    public void Approve()
    {
        if (Status != AffiliateStatus.Pending)
            throw new DomainException("Only pending applications can be approved");

        Status = AffiliateStatus.Active;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != AffiliateStatus.Pending)
            throw new DomainException("Only pending applications can be rejected");

        Status = AffiliateStatus.Rejected;
    }

    public void Suspend(string reason)
    {
        if (Status != AffiliateStatus.Active)
            throw new DomainException("Only active affiliates can be suspended");

        Status = AffiliateStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status != AffiliateStatus.Suspended)
            throw new DomainException("Only suspended affiliates can be reactivated");

        Status = AffiliateStatus.Active;
    }

    #endregion

    #region Referral Tracking

    public Referral TrackReferral(string ipAddress, string? userAgent, string? landingPage)
    {
        if (Status != AffiliateStatus.Active)
            throw new DomainException("Affiliate is not active");

        var referral = new Referral(
            Guid.NewGuid(),
            ipAddress,
            userAgent,
            landingPage,
            DateTime.UtcNow.AddDays(CookieDurationDays));

        _referrals.Add(referral);
        TotalReferrals++;
        return referral;
    }

    public void ConvertReferral(Guid referralId, Guid orderId, Guid customerId)
    {
        var referral = _referrals.FirstOrDefault(r => r.Id == referralId)
            ?? throw new DomainException("Referral not found");

        if (referral.IsConverted)
            throw new DomainException("Referral already converted");

        if (referral.IsExpired)
            throw new DomainException("Referral has expired");

        referral.MarkConverted(orderId, customerId);
        TotalConversions++;
    }

    #endregion

    #region Commission Management

    public Commission EarnCommission(Guid orderId, Money orderAmount, string description)
    {
        if (Status != AffiliateStatus.Active)
            throw new DomainException("Affiliate is not active");

        var commissionAmount = new Money(
            Math.Round(orderAmount.Amount * CommissionRate / 100, 2),
            orderAmount.Currency);

        var commission = new Commission(
            Guid.NewGuid(),
            orderId,
            orderAmount,
            commissionAmount,
            CommissionRate,
            description);

        _commissions.Add(commission);
        PendingEarnings = PendingEarnings.Add(commissionAmount);
        TotalEarnings = TotalEarnings.Add(commissionAmount);

        UpdateTier();
        return commission;
    }

    public void ApproveCommission(Guid commissionId)
    {
        var commission = _commissions.FirstOrDefault(c => c.Id == commissionId)
            ?? throw new DomainException("Commission not found");

        commission.Approve();
    }

    public void RejectCommission(Guid commissionId, string reason)
    {
        var commission = _commissions.FirstOrDefault(c => c.Id == commissionId)
            ?? throw new DomainException("Commission not found");

        commission.Reject(reason);
        PendingEarnings = PendingEarnings.Subtract(commission.CommissionAmount);
        TotalEarnings = TotalEarnings.Subtract(commission.CommissionAmount);
    }

    public void ReverseCommission(Guid commissionId, string reason)
    {
        var commission = _commissions.FirstOrDefault(c => c.Id == commissionId)
            ?? throw new DomainException("Commission not found");

        if (commission.Status == CommissionStatus.Paid)
            throw new DomainException("Cannot reverse paid commission");

        commission.Reverse(reason);
        PendingEarnings = PendingEarnings.Subtract(commission.CommissionAmount);
        TotalEarnings = TotalEarnings.Subtract(commission.CommissionAmount);
    }

    #endregion

    #region Payouts

    public Payout RequestPayout(PayoutMethod method)
    {
        if (Status != AffiliateStatus.Active)
            throw new DomainException("Affiliate is not active");

        if (PendingEarnings.Amount < MinimumPayoutThreshold.Amount)
            throw new DomainException($"Minimum payout threshold is {MinimumPayoutThreshold.Amount} {MinimumPayoutThreshold.Currency}");

        var approvedCommissions = _commissions
            .Where(c => c.Status == CommissionStatus.Approved)
            .ToList();

        if (!approvedCommissions.Any())
            throw new DomainException("No approved commissions to pay out");

        var payoutAmount = new Money(
            approvedCommissions.Sum(c => c.CommissionAmount.Amount),
            PendingEarnings.Currency);

        var payout = new Payout(
            Guid.NewGuid(),
            payoutAmount,
            method,
            approvedCommissions.Select(c => c.Id).ToList());

        _payouts.Add(payout);
        return payout;
    }

    public void ProcessPayout(Guid payoutId, string transactionReference)
    {
        var payout = _payouts.FirstOrDefault(p => p.Id == payoutId)
            ?? throw new DomainException("Payout not found");

        payout.MarkProcessed(transactionReference);

        foreach (var commissionId in payout.CommissionIds)
        {
            var commission = _commissions.First(c => c.Id == commissionId);
            commission.MarkPaid(payoutId);
        }

        PendingEarnings = PendingEarnings.Subtract(payout.Amount);
        PaidEarnings = PaidEarnings.Add(payout.Amount);
        LastPayoutAt = DateTime.UtcNow;
    }

    public void FailPayout(Guid payoutId, string reason)
    {
        var payout = _payouts.FirstOrDefault(p => p.Id == payoutId)
            ?? throw new DomainException("Payout not found");

        payout.MarkFailed(reason);
    }

    #endregion

    #region Tier Management

    private void UpdateTier()
    {
        var newTier = TotalEarnings.Amount switch
        {
            >= 10000 => AffiliateTier.Diamond,
            >= 5000 => AffiliateTier.Platinum,
            >= 2000 => AffiliateTier.Gold,
            >= 500 => AffiliateTier.Silver,
            _ => AffiliateTier.Bronze
        };

        if (newTier > Tier)
        {
            Tier = newTier;
            CommissionRate = newTier switch
            {
                AffiliateTier.Diamond => 15m,
                AffiliateTier.Platinum => 12m,
                AffiliateTier.Gold => 10m,
                AffiliateTier.Silver => 7m,
                _ => 5m
            };
        }
    }

    public void SetCustomCommissionRate(decimal rate)
    {
        if (rate <= 0 || rate > 50)
            throw new DomainException("Commission rate must be between 0 and 50%");

        CommissionRate = rate;
    }

    #endregion

    #region Settings

    public void UpdatePaymentInfo(string? paypalEmail, string? bankAccountInfo)
    {
        PaypalEmail = paypalEmail;
        BankAccountInfo = bankAccountInfo;
    }

    public void SetMinimumPayoutThreshold(Money threshold)
    {
        if (threshold.Amount < 10)
            throw new DomainException("Minimum threshold cannot be less than 10");

        MinimumPayoutThreshold = threshold;
    }

    public void SetCookieDuration(int days)
    {
        if (days < 1 || days > 365)
            throw new DomainException("Cookie duration must be between 1 and 365 days");

        CookieDurationDays = days;
    }

    public void RegenerateCode()
    {
        AffiliateCode = GenerateAffiliateCode();
    }

    #endregion

    private static string GenerateAffiliateCode()
    {
        return $"AFF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

public class Referral : Entity<Guid>
{
    public string IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? LandingPage { get; private set; }
    public bool IsConverted { get; private set; }
    public Guid? ConvertedOrderId { get; private set; }
    public Guid? ConvertedCustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConvertedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    internal Referral(Guid id, string ipAddress, string? userAgent, string? landingPage, DateTime expiresAt)
    {
        Id = id;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        LandingPage = landingPage;
        IsConverted = false;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    private Referral() { }

    internal void MarkConverted(Guid orderId, Guid customerId)
    {
        IsConverted = true;
        ConvertedOrderId = orderId;
        ConvertedCustomerId = customerId;
        ConvertedAt = DateTime.UtcNow;
    }
}

public class Commission : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Money OrderAmount { get; private set; }
    public Money CommissionAmount { get; private set; }
    public decimal CommissionRate { get; private set; }
    public string Description { get; private set; }
    public CommissionStatus Status { get; private set; }
    public Guid? PayoutId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    internal Commission(
        Guid id,
        Guid orderId,
        Money orderAmount,
        Money commissionAmount,
        decimal commissionRate,
        string description)
    {
        Id = id;
        OrderId = orderId;
        OrderAmount = orderAmount;
        CommissionAmount = commissionAmount;
        CommissionRate = commissionRate;
        Description = description;
        Status = CommissionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    private Commission() { }

    internal void Approve()
    {
        Status = CommissionStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
    }

    internal void Reject(string reason)
    {
        Status = CommissionStatus.Rejected;
        RejectionReason = reason;
    }

    internal void Reverse(string reason)
    {
        Status = CommissionStatus.Reversed;
        RejectionReason = reason;
    }

    internal void MarkPaid(Guid payoutId)
    {
        Status = CommissionStatus.Paid;
        PayoutId = payoutId;
        PaidAt = DateTime.UtcNow;
    }
}

public class Payout : Entity<Guid>
{
    public Money Amount { get; private set; }
    public PayoutMethod Method { get; private set; }
    public PayoutStatus Status { get; private set; }
    public string? TransactionReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private readonly List<Guid> _commissionIds = new();
    public IReadOnlyCollection<Guid> CommissionIds => _commissionIds.AsReadOnly();

    internal Payout(Guid id, Money amount, PayoutMethod method, List<Guid> commissionIds)
    {
        Id = id;
        Amount = amount;
        Method = method;
        Status = PayoutStatus.Pending;
        RequestedAt = DateTime.UtcNow;
        _commissionIds.AddRange(commissionIds);
    }

    private Payout() { }

    internal void MarkProcessed(string transactionReference)
    {
        Status = PayoutStatus.Completed;
        TransactionReference = transactionReference;
        ProcessedAt = DateTime.UtcNow;
    }

    internal void MarkFailed(string reason)
    {
        Status = PayoutStatus.Failed;
        FailureReason = reason;
    }
}

public enum AffiliateStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Rejected = 4,
    Closed = 5
}

public enum AffiliateTier
{
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4,
    Diamond = 5
}

public enum CommissionStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Reversed = 4,
    Paid = 5
}

public enum PayoutMethod
{
    PayPal = 1,
    BankTransfer = 2,
    Check = 3,
    StoreCredit = 4
}

public enum PayoutStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}
