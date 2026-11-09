using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.CouponAggregate;

/// <summary>
/// Coupon Aggregate - Single-use discount codes
/// 
/// BUSINESS RULES:
/// - Unique coupon codes with usage limits
/// - Percentage or fixed amount discounts
/// - Minimum order requirements
/// - Product/category restrictions
/// - Customer usage tracking
/// - Bulk coupon generation
/// </summary>
public class Coupon : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CouponType Type { get; private set; }
    public CouponStatus Status { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public Money? MaxDiscountAmount { get; private set; }
    public Money? MinimumOrderAmount { get; private set; }
    public int? MaxUsageCount { get; private set; }
    public int? MaxUsagePerCustomer { get; private set; }
    public int CurrentUsageCount { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public bool IsFirstOrderOnly { get; private set; }
    public bool IsStackable { get; private set; }
    public Guid? CampaignId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Guid> _eligibleProductIds = new();
    public IReadOnlyCollection<Guid> EligibleProductIds => _eligibleProductIds.AsReadOnly();

    private readonly List<Guid> _eligibleCategoryIds = new();
    public IReadOnlyCollection<Guid> EligibleCategoryIds => _eligibleCategoryIds.AsReadOnly();

    private readonly List<Guid> _excludedProductIds = new();
    public IReadOnlyCollection<Guid> ExcludedProductIds => _excludedProductIds.AsReadOnly();

    private readonly List<CouponUsage> _usages = new();
    public IReadOnlyCollection<CouponUsage> Usages => _usages.AsReadOnly();

    public bool IsActive => Status == CouponStatus.Active && IsWithinValidityPeriod && !IsExhausted;
    public bool IsWithinValidityPeriod =>
        (!ValidFrom.HasValue || DateTime.UtcNow >= ValidFrom.Value) &&
        (!ValidUntil.HasValue || DateTime.UtcNow <= ValidUntil.Value);
    public bool IsExhausted => MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount.Value;
    public int RemainingUsages => MaxUsageCount.HasValue ? MaxUsageCount.Value - CurrentUsageCount : int.MaxValue;

    private Coupon() { }

    public static Coupon CreatePercentage(
        string code,
        string name,
        decimal percentage,
        Money? maxDiscount = null,
        int? maxUsage = null)
    {
        if (percentage <= 0 || percentage > 100)
            throw new DomainException("Percentage must be between 0 and 100");

        return Create(code, name, DiscountType.Percentage, percentage, maxDiscount, maxUsage);
    }

    public static Coupon CreateFixedAmount(
        string code,
        string name,
        Money discountAmount,
        int? maxUsage = null)
    {
        return Create(code, name, DiscountType.FixedAmount, discountAmount.Amount, null, maxUsage);
    }

    public static Coupon CreateFreeShipping(
        string code,
        string name,
        int? maxUsage = null)
    {
        return Create(code, name, DiscountType.FreeShipping, 0, null, maxUsage);
    }

    private static Coupon Create(
        string code,
        string name,
        DiscountType discountType,
        decimal discountValue,
        Money? maxDiscount,
        int? maxUsage)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Coupon code is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Coupon name is required");

        return new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant().Trim(),
            Name = name,
            Type = CouponType.Standard,
            Status = CouponStatus.Draft,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscount,
            MaxUsageCount = maxUsage,
            MaxUsagePerCustomer = 1,
            CurrentUsageCount = 0,
            IsFirstOrderOnly = false,
            IsStackable = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static List<Coupon> GenerateBulk(
        string prefix,
        string name,
        DiscountType discountType,
        decimal discountValue,
        int quantity,
        int usagePerCoupon = 1)
    {
        var coupons = new List<Coupon>();
        for (int i = 0; i < quantity; i++)
        {
            var code = $"{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            var coupon = Create(code, name, discountType, discountValue, null, usagePerCoupon);
            coupon.Type = CouponType.SingleUse;
            coupons.Add(coupon);
        }
        return coupons;
    }

    #region Validation

    public CouponValidationResult Validate(
        Guid customerId,
        Money orderTotal,
        IEnumerable<Guid> productIds,
        IEnumerable<Guid> categoryIds,
        bool isFirstOrder)
    {
        if (!IsActive)
            return CouponValidationResult.Invalid("Coupon is not active");

        if (IsFirstOrderOnly && !isFirstOrder)
            return CouponValidationResult.Invalid("Coupon is valid for first orders only");

        if (MinimumOrderAmount != null && orderTotal.Amount < MinimumOrderAmount.Amount)
            return CouponValidationResult.Invalid($"Minimum order amount is {MinimumOrderAmount.Amount} {MinimumOrderAmount.Currency}");

        if (MaxUsagePerCustomer.HasValue)
        {
            var customerUsage = _usages.Count(u => u.CustomerId == customerId);
            if (customerUsage >= MaxUsagePerCustomer.Value)
                return CouponValidationResult.Invalid("You have already used this coupon the maximum number of times");
        }

        // Product restrictions
        if (_eligibleProductIds.Any() && !productIds.Any(p => _eligibleProductIds.Contains(p)))
            return CouponValidationResult.Invalid("Coupon is not valid for these products");

        if (_excludedProductIds.Any() && productIds.All(p => _excludedProductIds.Contains(p)))
            return CouponValidationResult.Invalid("Coupon cannot be applied to these products");

        // Category restrictions
        if (_eligibleCategoryIds.Any() && !categoryIds.Any(c => _eligibleCategoryIds.Contains(c)))
            return CouponValidationResult.Invalid("Coupon is not valid for these categories");

        var discount = CalculateDiscount(orderTotal);
        return CouponValidationResult.Valid(discount);
    }

    public Money CalculateDiscount(Money orderTotal)
    {
        var discount = DiscountType switch
        {
            DiscountType.Percentage => new Money(
                Math.Round(orderTotal.Amount * DiscountValue / 100, 2),
                orderTotal.Currency),
            DiscountType.FixedAmount => new Money(DiscountValue, orderTotal.Currency),
            DiscountType.FreeShipping => Money.Zero(orderTotal.Currency),
            _ => Money.Zero(orderTotal.Currency)
        };

        // Cap at max discount
        if (MaxDiscountAmount != null && discount.Amount > MaxDiscountAmount.Amount)
            discount = MaxDiscountAmount;

        // Can't exceed order total
        if (discount.Amount > orderTotal.Amount)
            discount = orderTotal;

        return discount;
    }

    #endregion

    #region Usage

    public CouponUsage Apply(Guid orderId, Guid customerId, Money discountApplied)
    {
        if (!IsActive)
            throw new DomainException("Coupon is not active");

        var usage = new CouponUsage(
            Guid.NewGuid(),
            orderId,
            customerId,
            discountApplied);

        _usages.Add(usage);
        CurrentUsageCount++;

        if (IsExhausted)
            Status = CouponStatus.Exhausted;

        return usage;
    }

    public void ReverseUsage(Guid orderId)
    {
        var usage = _usages.FirstOrDefault(u => u.OrderId == orderId)
            ?? throw new DomainException("Usage not found");

        usage.MarkReversed();
        CurrentUsageCount--;

        if (Status == CouponStatus.Exhausted)
            Status = CouponStatus.Active;
    }

    #endregion

    #region Configuration

    public void SetValidityPeriod(DateTime? from, DateTime? until)
    {
        if (from.HasValue && until.HasValue && from.Value >= until.Value)
            throw new DomainException("End date must be after start date");

        ValidFrom = from;
        ValidUntil = until;
    }

    public void SetMinimumOrderAmount(Money minimum)
    {
        MinimumOrderAmount = minimum;
    }

    public void SetMaxUsage(int? total, int? perCustomer)
    {
        if (total.HasValue && total.Value < CurrentUsageCount)
            throw new DomainException("Max usage cannot be less than current usage");

        MaxUsageCount = total;
        MaxUsagePerCustomer = perCustomer;
    }

    public void SetFirstOrderOnly(bool firstOrderOnly)
    {
        IsFirstOrderOnly = firstOrderOnly;
    }

    public void SetStackable(bool stackable)
    {
        IsStackable = stackable;
    }

    public void AddEligibleProducts(params Guid[] productIds)
    {
        foreach (var id in productIds)
            if (!_eligibleProductIds.Contains(id))
                _eligibleProductIds.Add(id);
    }

    public void AddEligibleCategories(params Guid[] categoryIds)
    {
        foreach (var id in categoryIds)
            if (!_eligibleCategoryIds.Contains(id))
                _eligibleCategoryIds.Add(id);
    }

    public void AddExcludedProducts(params Guid[] productIds)
    {
        foreach (var id in productIds)
            if (!_excludedProductIds.Contains(id))
                _excludedProductIds.Add(id);
    }

    public void ClearRestrictions()
    {
        _eligibleProductIds.Clear();
        _eligibleCategoryIds.Clear();
        _excludedProductIds.Clear();
    }

    #endregion

    #region Lifecycle

    public void Activate()
    {
        if (Status == CouponStatus.Expired || Status == CouponStatus.Exhausted)
            throw new DomainException("Cannot activate expired or exhausted coupon");

        Status = CouponStatus.Active;
    }

    public void Deactivate() => Status = CouponStatus.Inactive;
    public void Expire() => Status = CouponStatus.Expired;

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");

        Name = name;
        Description = description;
    }

    #endregion
}

public class CouponUsage : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money DiscountApplied { get; private set; }
    public bool IsReversed { get; private set; }
    public DateTime UsedAt { get; private set; }
    public DateTime? ReversedAt { get; private set; }

    internal CouponUsage(Guid id, Guid orderId, Guid customerId, Money discountApplied)
    {
        Id = id;
        OrderId = orderId;
        CustomerId = customerId;
        DiscountApplied = discountApplied;
        IsReversed = false;
        UsedAt = DateTime.UtcNow;
    }

    private CouponUsage() { }

    internal void MarkReversed()
    {
        IsReversed = true;
        ReversedAt = DateTime.UtcNow;
    }
}

public class CouponValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public Money? DiscountAmount { get; }

    private CouponValidationResult(bool isValid, string? error, Money? discount)
    {
        IsValid = isValid;
        ErrorMessage = error;
        DiscountAmount = discount;
    }

    public static CouponValidationResult Valid(Money discount) => new(true, null, discount);
    public static CouponValidationResult Invalid(string error) => new(false, error, null);
}

public enum CouponType
{
    Standard = 1,
    SingleUse = 2,
    Referral = 3,
    Loyalty = 4,
    Welcome = 5,
    Birthday = 6
}

public enum CouponStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Expired = 4,
    Exhausted = 5
}

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2,
    FreeShipping = 3
}
