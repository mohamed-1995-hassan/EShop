using EShop.Domain.Aggregates.PromotionAggregate;
using EShop.Domain.Common;

namespace EShop.Domain.Events;

public class PromotionCreatedEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Code { get; }
    public string Name { get; }
    public PromotionType Type { get; }
    public DiscountType DiscountType { get; }
    public decimal DiscountValue { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public PromotionCreatedEvent(Guid promotionId, string code, string name, PromotionType type, DiscountType discountType, decimal discountValue, DateTime startDate, DateTime endDate)
    {
        PromotionId = promotionId;
        Code = code;
        Name = name;
        Type = type;
        DiscountType = discountType;
        DiscountValue = discountValue;
        StartDate = startDate;
        EndDate = endDate;
    }
}

public class PromotionActivatedEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Code { get; }
    public DateTime StartDate { get; }

    public PromotionActivatedEvent(Guid promotionId, string code, DateTime startDate)
    {
        PromotionId = promotionId;
        Code = code;
        StartDate = startDate;
    }
}

public class PromotionDeactivatedEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Code { get; }
    public string? Reason { get; }

    public PromotionDeactivatedEvent(Guid promotionId, string code, string? reason)
    {
        PromotionId = promotionId;
        Code = code;
        Reason = reason;
    }
}

public class PromotionUsedEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Code { get; }
    public Guid CustomerId { get; }
    public Guid OrderId { get; }
    public decimal DiscountApplied { get; }
    public int CurrentUsageCount { get; }
    public int? MaxUsageTotal { get; }

    public PromotionUsedEvent(Guid promotionId, string code, Guid customerId, Guid orderId, decimal discountApplied, int currentUsageCount, int? maxUsageTotal)
    {
        PromotionId = promotionId;
        Code = code;
        CustomerId = customerId;
        OrderId = orderId;
        DiscountApplied = discountApplied;
        CurrentUsageCount = currentUsageCount;
        MaxUsageTotal = maxUsageTotal;
    }
}

public class PromotionUsageLimitReachedEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Code { get; }
    public int TotalUsages { get; }

    public PromotionUsageLimitReachedEvent(Guid promotionId, string code, int totalUsages)
    {
        PromotionId = promotionId;
        Code = code;
        TotalUsages = totalUsages;
    }
}

public class PromotionExpiredEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Code { get; }
    public DateTime EndDate { get; }
    public int TotalUsages { get; }
    public decimal TotalDiscountGiven { get; }

    public PromotionExpiredEvent(Guid promotionId, string code, DateTime endDate, int totalUsages, decimal totalDiscountGiven)
    {
        PromotionId = promotionId;
        Code = code;
        EndDate = endDate;
        TotalUsages = totalUsages;
        TotalDiscountGiven = totalDiscountGiven;
    }
}

public class FlashSaleStartedEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Name { get; }
    public decimal DiscountPercentage { get; }
    public IEnumerable<Guid> ProductIds { get; }
    public DateTime EndDate { get; }

    public FlashSaleStartedEvent(Guid promotionId, string name, decimal discountPercentage, IEnumerable<Guid> productIds, DateTime endDate)
    {
        PromotionId = promotionId;
        Name = name;
        DiscountPercentage = discountPercentage;
        ProductIds = productIds;
        EndDate = endDate;
    }
}

public class FlashSaleEndedEvent : DomainEvent
{
    public Guid PromotionId { get; }
    public string Name { get; }
    public int TotalOrders { get; }
    public decimal TotalRevenue { get; }

    public FlashSaleEndedEvent(Guid promotionId, string name, int totalOrders, decimal totalRevenue)
    {
        PromotionId = promotionId;
        Name = name;
        TotalOrders = totalOrders;
        TotalRevenue = totalRevenue;
    }
}
