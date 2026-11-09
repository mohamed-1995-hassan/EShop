using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.AnalyticsAggregate;

/// <summary>
/// Analytics Aggregate - Sales reports and customer insights
/// 
/// BUSINESS RULES:
/// - Track sales metrics over time
/// - Customer behavior analytics
/// - Product performance tracking
/// - Conversion funnel analysis
/// - Revenue forecasting
/// </summary>
public class SalesReport : AggregateRoot<Guid>
{
    public ReportPeriod Period { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ReportStatus Status { get; private set; }
    public int TotalOrders { get; private set; }
    public int TotalItems { get; private set; }
    public int UniqueCustomers { get; private set; }
    public int NewCustomers { get; private set; }
    public int ReturningCustomers { get; private set; }
    public Money TotalRevenue { get; private set; } = null!;
    public Money TotalDiscounts { get; private set; } = null!;
    public Money TotalShipping { get; private set; } = null!;
    public Money TotalTax { get; private set; } = null!;
    public Money NetRevenue { get; private set; } = null!;
    public Money AverageOrderValue { get; private set; } = null!;
    public decimal ConversionRate { get; private set; }
    public decimal CartAbandonmentRate { get; private set; }
    public decimal ReturnRate { get; private set; }
    public DateTime GeneratedAt { get; private set; }

    private readonly List<CategorySales> _categorySales = new();
    public IReadOnlyCollection<CategorySales> CategorySales => _categorySales.AsReadOnly();

    private readonly List<ProductSales> _topProducts = new();
    public IReadOnlyCollection<ProductSales> TopProducts => _topProducts.AsReadOnly();

    private readonly List<DailySales> _dailyBreakdown = new();
    public IReadOnlyCollection<DailySales> DailyBreakdown => _dailyBreakdown.AsReadOnly();

    private SalesReport() { }

    public static SalesReport Create(ReportPeriod period, DateTime startDate, DateTime endDate, string currency)
    {
        if (startDate >= endDate)
            throw new DomainException("Start date must be before end date");

        return new SalesReport
        {
            Id = Guid.NewGuid(),
            Period = period,
            StartDate = startDate,
            EndDate = endDate,
            Status = ReportStatus.Generating,
            TotalOrders = 0,
            TotalItems = 0,
            UniqueCustomers = 0,
            NewCustomers = 0,
            ReturningCustomers = 0,
            TotalRevenue = Money.Zero(currency),
            TotalDiscounts = Money.Zero(currency),
            TotalShipping = Money.Zero(currency),
            TotalTax = Money.Zero(currency),
            NetRevenue = Money.Zero(currency),
            AverageOrderValue = Money.Zero(currency),
            ConversionRate = 0,
            CartAbandonmentRate = 0,
            ReturnRate = 0,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public void SetMetrics(
        int totalOrders,
        int totalItems,
        int uniqueCustomers,
        int newCustomers,
        Money totalRevenue,
        Money totalDiscounts,
        Money totalShipping,
        Money totalTax,
        decimal conversionRate,
        decimal cartAbandonmentRate,
        decimal returnRate)
    {
        TotalOrders = totalOrders;
        TotalItems = totalItems;
        UniqueCustomers = uniqueCustomers;
        NewCustomers = newCustomers;
        ReturningCustomers = uniqueCustomers - newCustomers;
        TotalRevenue = totalRevenue;
        TotalDiscounts = totalDiscounts;
        TotalShipping = totalShipping;
        TotalTax = totalTax;
        NetRevenue = totalRevenue.Subtract(totalDiscounts);
        AverageOrderValue = totalOrders > 0 
            ? new Money(totalRevenue.Amount / totalOrders, totalRevenue.Currency)
            : Money.Zero(totalRevenue.Currency);
        ConversionRate = conversionRate;
        CartAbandonmentRate = cartAbandonmentRate;
        ReturnRate = returnRate;
    }

    public void AddCategorySales(Guid categoryId, string categoryName, int orderCount, Money revenue)
    {
        _categorySales.Add(new CategorySales(categoryId, categoryName, orderCount, revenue));
    }

    public void AddTopProduct(Guid productId, string productName, int unitsSold, Money revenue)
    {
        _topProducts.Add(new ProductSales(productId, productName, unitsSold, revenue, _topProducts.Count + 1));
    }

    public void AddDailyBreakdown(DateTime date, int orders, Money revenue)
    {
        _dailyBreakdown.Add(new DailySales(date, orders, revenue));
    }

    public void Complete()
    {
        Status = ReportStatus.Completed;
        GeneratedAt = DateTime.UtcNow;
    }

    public void Fail(string error)
    {
        Status = ReportStatus.Failed;
    }
}

public record CategorySales(Guid CategoryId, string CategoryName, int OrderCount, Money Revenue);
public record ProductSales(Guid ProductId, string ProductName, int UnitsSold, Money Revenue, int Rank);
public record DailySales(DateTime Date, int Orders, Money Revenue);

/// <summary>
/// Customer Insight - Individual customer analytics
/// </summary>
public class CustomerInsight : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public CustomerSegment Segment { get; private set; }
    public int TotalOrders { get; private set; }
    public Money LifetimeValue { get; private set; } = null!;
    public Money AverageOrderValue { get; private set; } = null!;
    public int DaysSinceFirstOrder { get; private set; }
    public int DaysSinceLastOrder { get; private set; }
    public decimal PurchaseFrequency { get; private set; }
    public int TotalProductsViewed { get; private set; }
    public int TotalProductsPurchased { get; private set; }
    public decimal CartToOrderRate { get; private set; }
    public bool IsAtRisk { get; private set; }
    public DateTime? PredictedNextPurchase { get; private set; }
    public DateTime LastUpdated { get; private set; }

    private readonly List<Guid> _preferredCategoryIds = new();
    public IReadOnlyCollection<Guid> PreferredCategoryIds => _preferredCategoryIds.AsReadOnly();

    private readonly List<Guid> _frequentlyBoughtProductIds = new();
    public IReadOnlyCollection<Guid> FrequentlyBoughtProductIds => _frequentlyBoughtProductIds.AsReadOnly();

    private CustomerInsight() { }

    public static CustomerInsight Create(Guid customerId, string currency)
    {
        return new CustomerInsight
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Segment = CustomerSegment.New,
            TotalOrders = 0,
            LifetimeValue = Money.Zero(currency),
            AverageOrderValue = Money.Zero(currency),
            DaysSinceFirstOrder = 0,
            DaysSinceLastOrder = 0,
            PurchaseFrequency = 0,
            TotalProductsViewed = 0,
            TotalProductsPurchased = 0,
            CartToOrderRate = 0,
            IsAtRisk = false,
            LastUpdated = DateTime.UtcNow
        };
    }

    public void UpdatePurchaseMetrics(
        int totalOrders,
        Money lifetimeValue,
        DateTime firstOrderDate,
        DateTime lastOrderDate)
    {
        TotalOrders = totalOrders;
        LifetimeValue = lifetimeValue;
        AverageOrderValue = totalOrders > 0
            ? new Money(lifetimeValue.Amount / totalOrders, lifetimeValue.Currency)
            : Money.Zero(lifetimeValue.Currency);

        DaysSinceFirstOrder = (int)(DateTime.UtcNow - firstOrderDate).TotalDays;
        DaysSinceLastOrder = (int)(DateTime.UtcNow - lastOrderDate).TotalDays;

        if (DaysSinceFirstOrder > 0 && totalOrders > 1)
            PurchaseFrequency = (decimal)DaysSinceFirstOrder / (totalOrders - 1);

        UpdateSegment();
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateBehaviorMetrics(int productsViewed, int productsPurchased, decimal cartToOrderRate)
    {
        TotalProductsViewed = productsViewed;
        TotalProductsPurchased = productsPurchased;
        CartToOrderRate = cartToOrderRate;
        LastUpdated = DateTime.UtcNow;
    }

    public void SetPreferredCategories(IEnumerable<Guid> categoryIds)
    {
        _preferredCategoryIds.Clear();
        _preferredCategoryIds.AddRange(categoryIds.Take(5));
    }

    public void SetFrequentlyBoughtProducts(IEnumerable<Guid> productIds)
    {
        _frequentlyBoughtProductIds.Clear();
        _frequentlyBoughtProductIds.AddRange(productIds.Take(10));
    }

    public void SetPredictedNextPurchase(DateTime? prediction)
    {
        PredictedNextPurchase = prediction;
    }

    private void UpdateSegment()
    {
        // RFM-based segmentation
        IsAtRisk = DaysSinceLastOrder > 90;

        Segment = (TotalOrders, LifetimeValue.Amount, DaysSinceLastOrder) switch
        {
            ( >= 10, >= 1000, < 30) => CustomerSegment.Champion,
            ( >= 5, >= 500, < 60) => CustomerSegment.Loyal,
            ( >= 3, _, < 30) => CustomerSegment.Potential,
            (1, _, < 30) => CustomerSegment.New,
            ( >= 3, >= 500, >= 90) => CustomerSegment.AtRisk,
            ( >= 1, _, >= 180) => CustomerSegment.Dormant,
            _ => CustomerSegment.Regular
        };
    }
}

/// <summary>
/// Product Performance - Product analytics
/// </summary>
public class ProductPerformance : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public int TotalViews { get; private set; }
    public int TotalAddedToCart { get; private set; }
    public int TotalPurchases { get; private set; }
    public int TotalReturns { get; private set; }
    public int TotalReviews { get; private set; }
    public decimal AverageRating { get; private set; }
    public decimal ViewToCartRate { get; private set; }
    public decimal CartToPurchaseRate { get; private set; }
    public decimal ReturnRate { get; private set; }
    public Money TotalRevenue { get; private set; } = null!;
    public int CurrentStock { get; private set; }
    public int DaysOfStock { get; private set; }
    public bool IsTopSeller { get; private set; }
    public bool IsSlowMover { get; private set; }
    public DateTime LastUpdated { get; private set; }

    private ProductPerformance() { }

    public static ProductPerformance Create(Guid productId, string currency)
    {
        return new ProductPerformance
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            TotalViews = 0,
            TotalAddedToCart = 0,
            TotalPurchases = 0,
            TotalReturns = 0,
            TotalReviews = 0,
            AverageRating = 0,
            ViewToCartRate = 0,
            CartToPurchaseRate = 0,
            ReturnRate = 0,
            TotalRevenue = Money.Zero(currency),
            CurrentStock = 0,
            DaysOfStock = 0,
            IsTopSeller = false,
            IsSlowMover = false,
            LastUpdated = DateTime.UtcNow
        };
    }

    public void RecordView() { TotalViews++; UpdateRates(); }
    public void RecordAddToCart() { TotalAddedToCart++; UpdateRates(); }
    public void RecordPurchase(Money amount) { TotalPurchases++; TotalRevenue = TotalRevenue.Add(amount); UpdateRates(); }
    public void RecordReturn() { TotalReturns++; UpdateRates(); }

    public void UpdateReviewMetrics(int totalReviews, decimal averageRating)
    {
        TotalReviews = totalReviews;
        AverageRating = averageRating;
    }

    public void UpdateStockMetrics(int currentStock, decimal dailySalesRate)
    {
        CurrentStock = currentStock;
        DaysOfStock = dailySalesRate > 0 ? (int)(currentStock / dailySalesRate) : int.MaxValue;
    }

    public void SetTopSeller(bool isTopSeller) { IsTopSeller = isTopSeller; }
    public void SetSlowMover(bool isSlowMover) { IsSlowMover = isSlowMover; }

    private void UpdateRates()
    {
        ViewToCartRate = TotalViews > 0 ? (decimal)TotalAddedToCart / TotalViews * 100 : 0;
        CartToPurchaseRate = TotalAddedToCart > 0 ? (decimal)TotalPurchases / TotalAddedToCart * 100 : 0;
        ReturnRate = TotalPurchases > 0 ? (decimal)TotalReturns / TotalPurchases * 100 : 0;
        LastUpdated = DateTime.UtcNow;
    }
}

public enum ReportPeriod
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Yearly = 5,
    Custom = 6
}

public enum ReportStatus
{
    Generating = 1,
    Completed = 2,
    Failed = 3
}

public enum CustomerSegment
{
    New = 1,
    Regular = 2,
    Potential = 3,
    Loyal = 4,
    Champion = 5,
    AtRisk = 6,
    Dormant = 7,
    Lost = 8
}
