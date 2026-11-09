using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.PromotionAggregate;

/// <summary>
/// Promotion Aggregate Root - Discounts, Coupons, Flash Sales
/// 
/// BUSINESS RULES:
/// - Active promotions within valid date range only
/// - Usage limits per customer and total
/// - Minimum order value requirements
/// - Cannot stack certain promotion types
/// - Product/Category specific or store-wide
/// </summary>
public class Promotion : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PromotionType Type { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public decimal? MinimumOrderValue { get; private set; }
    public int? MaxUsageTotal { get; private set; }
    public int? MaxUsagePerCustomer { get; private set; }
    public int CurrentUsageCount { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsStackable { get; private set; }
    public PromotionScope Scope { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Applicable products/categories
    private readonly List<Guid> _applicableProductIds = new();
    public IReadOnlyCollection<Guid> ApplicableProductIds => _applicableProductIds.AsReadOnly();

    private readonly List<Guid> _applicableCategoryIds = new();
    public IReadOnlyCollection<Guid> ApplicableCategoryIds => _applicableCategoryIds.AsReadOnly();

    // Customer tier restrictions
    private readonly List<int> _eligibleTiers = new();
    public IReadOnlyCollection<int> EligibleTiers => _eligibleTiers.AsReadOnly();

    // Usage tracking
    private readonly List<PromotionUsage> _usages = new();
    public IReadOnlyCollection<PromotionUsage> Usages => _usages.AsReadOnly();

    private Promotion() { }

    public static Promotion CreatePercentageDiscount(
        string code,
        string name,
        decimal percentage,
        DateTime startDate,
        DateTime endDate,
        decimal? maxDiscountAmount = null,
        decimal? minimumOrderValue = null)
    {
        if (percentage <= 0 || percentage > 100)
            throw new DomainException("Percentage must be between 0 and 100");

        return CreateBase(code, name, PromotionType.Coupon, DiscountType.Percentage, 
            percentage, startDate, endDate, maxDiscountAmount, minimumOrderValue);
    }

    public static Promotion CreateFixedDiscount(
        string code,
        string name,
        decimal amount,
        string currency,
        DateTime startDate,
        DateTime endDate,
        decimal? minimumOrderValue = null)
    {
        if (amount <= 0)
            throw new DomainException("Discount amount must be greater than zero");

        return CreateBase(code, name, PromotionType.Coupon, DiscountType.FixedAmount, 
            amount, startDate, endDate, null, minimumOrderValue);
    }

    public static Promotion CreateFreeShipping(
        string code,
        string name,
        DateTime startDate,
        DateTime endDate,
        decimal? minimumOrderValue = null)
    {
        return CreateBase(code, name, PromotionType.Coupon, DiscountType.FreeShipping, 
            0, startDate, endDate, null, minimumOrderValue);
    }

    public static Promotion CreateFlashSale(
        string name,
        decimal percentage,
        DateTime startDate,
        DateTime endDate,
        IEnumerable<Guid> productIds)
    {
        var promo = CreateBase($"FLASH-{DateTime.UtcNow:yyyyMMddHHmm}", name, 
            PromotionType.FlashSale, DiscountType.Percentage, 
            percentage, startDate, endDate, null, null);

        foreach (var productId in productIds)
            promo._applicableProductIds.Add(productId);

        promo.Scope = PromotionScope.SpecificProducts;
        return promo;
    }

    public static Promotion CreateBuyXGetY(
        string code,
        string name,
        int buyQuantity,
        int getQuantity,
        DateTime startDate,
        DateTime endDate)
    {
        var promo = CreateBase(code, name, PromotionType.BuyXGetY, DiscountType.Percentage, 
            100, startDate, endDate, null, null);
        // Store buy/get quantities in description for simplicity
        promo.Description = $"Buy {buyQuantity} Get {getQuantity} Free";
        return promo;
    }

    private static Promotion CreateBase(
        string code,
        string name,
        PromotionType type,
        DiscountType discountType,
        decimal discountValue,
        DateTime startDate,
        DateTime endDate,
        decimal? maxDiscountAmount,
        decimal? minimumOrderValue)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Promotion code is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Promotion name is required");
        if (startDate >= endDate)
            throw new DomainException("End date must be after start date");

        return new Promotion
        {
            Id = Guid.NewGuid(),
            Code = code.ToUpperInvariant(),
            Name = name,
            Type = type,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscountAmount,
            MinimumOrderValue = minimumOrderValue,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            IsStackable = false,
            Scope = PromotionScope.StoreWide,
            CreatedAt = DateTime.UtcNow,
            CurrentUsageCount = 0
        };
    }

    #region Configuration

    public void SetUsageLimits(int? maxTotal, int? maxPerCustomer)
    {
        MaxUsageTotal = maxTotal;
        MaxUsagePerCustomer = maxPerCustomer;
    }

    public void SetEligibleTiers(params int[] tiers)
    {
        _eligibleTiers.Clear();
        _eligibleTiers.AddRange(tiers);
    }

    public void AddApplicableProduct(Guid productId)
    {
        if (!_applicableProductIds.Contains(productId))
        {
            _applicableProductIds.Add(productId);
            Scope = PromotionScope.SpecificProducts;
        }
    }

    public void AddApplicableCategory(Guid categoryId)
    {
        if (!_applicableCategoryIds.Contains(categoryId))
        {
            _applicableCategoryIds.Add(categoryId);
            Scope = PromotionScope.SpecificCategories;
        }
    }

    public void AllowStacking() => IsStackable = true;
    public void DisallowStacking() => IsStackable = false;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void ExtendEndDate(DateTime newEndDate)
    {
        if (newEndDate <= EndDate)
            throw new DomainException("New end date must be after current end date");
        EndDate = newEndDate;
    }

    #endregion

    #region Validation & Application

    public PromotionValidationResult Validate(
        Guid customerId,
        decimal orderTotal,
        int customerTier,
        IEnumerable<Guid> productIds)
    {
        var now = DateTime.UtcNow;

        if (!IsActive)
            return PromotionValidationResult.Invalid("Promotion is not active");

        if (now < StartDate)
            return PromotionValidationResult.Invalid("Promotion has not started yet");

        if (now > EndDate)
            return PromotionValidationResult.Invalid("Promotion has expired");

        if (MinimumOrderValue.HasValue && orderTotal < MinimumOrderValue.Value)
            return PromotionValidationResult.Invalid($"Minimum order value is {MinimumOrderValue.Value:N2}");

        if (MaxUsageTotal.HasValue && CurrentUsageCount >= MaxUsageTotal.Value)
            return PromotionValidationResult.Invalid("Promotion usage limit reached");

        if (MaxUsagePerCustomer.HasValue)
        {
            var customerUsageCount = _usages.Count(u => u.CustomerId == customerId);
            if (customerUsageCount >= MaxUsagePerCustomer.Value)
                return PromotionValidationResult.Invalid("You have reached the maximum usage for this promotion");
        }

        if (_eligibleTiers.Any() && !_eligibleTiers.Contains(customerTier))
            return PromotionValidationResult.Invalid("Your account tier is not eligible for this promotion");

        if (Scope == PromotionScope.SpecificProducts && _applicableProductIds.Any())
        {
            if (!productIds.Any(p => _applicableProductIds.Contains(p)))
                return PromotionValidationResult.Invalid("No eligible products in cart");
        }

        return PromotionValidationResult.Valid();
    }

    public Money CalculateDiscount(decimal eligibleAmount, string currency)
    {
        decimal discount = DiscountType switch
        {
            DiscountType.Percentage => eligibleAmount * (DiscountValue / 100),
            DiscountType.FixedAmount => Math.Min(DiscountValue, eligibleAmount),
            DiscountType.FreeShipping => 0, // Handled separately
            _ => 0
        };

        // Apply max discount cap
        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            discount = MaxDiscountAmount.Value;

        return new Money(discount, currency);
    }

    public void RecordUsage(Guid customerId, Guid orderId, decimal discountApplied)
    {
        _usages.Add(new PromotionUsage(Guid.NewGuid(), customerId, orderId, discountApplied));
        CurrentUsageCount++;
    }

    #endregion
}

public class PromotionUsage : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal DiscountApplied { get; private set; }
    public DateTime UsedAt { get; private set; }

    internal PromotionUsage(Guid id, Guid customerId, Guid orderId, decimal discountApplied)
    {
        Id = id;
        CustomerId = customerId;
        OrderId = orderId;
        DiscountApplied = discountApplied;
        UsedAt = DateTime.UtcNow;
    }

    private PromotionUsage() { } // EF Core
}

public enum PromotionType
{
    Coupon = 1,
    FlashSale = 2,
    BuyXGetY = 3,
    BundleDeal = 4,
    SeasonalSale = 5,
    LoyaltyReward = 6,
    ReferralDiscount = 7,
    FirstOrderDiscount = 8
}

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2,
    FreeShipping = 3
}

public enum PromotionScope
{
    StoreWide = 1,
    SpecificProducts = 2,
    SpecificCategories = 3
}

public class PromotionValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private PromotionValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static PromotionValidationResult Valid() => new(true);
    public static PromotionValidationResult Invalid(string message) => new(false, message);
}
