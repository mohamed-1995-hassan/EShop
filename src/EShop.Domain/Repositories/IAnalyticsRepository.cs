using EShop.Domain.Aggregates.AnalyticsAggregate;

namespace EShop.Domain.Repositories;

public interface ISalesReportRepository
{
    Task<SalesReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesReport?> GetByPeriodAsync(ReportPeriod period, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesReport>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task AddAsync(SalesReport report, CancellationToken cancellationToken = default);
    Task UpdateAsync(SalesReport report, CancellationToken cancellationToken = default);
}

public interface ICustomerInsightRepository
{
    Task<CustomerInsight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerInsight?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerInsight>> GetBySegmentAsync(CustomerSegment segment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerInsight>> GetAtRiskCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerInsight>> GetTopCustomersAsync(int count, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerInsight insight, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomerInsight insight, CancellationToken cancellationToken = default);
}

public interface IProductPerformanceRepository
{
    Task<ProductPerformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductPerformance?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductPerformance>> GetTopSellersAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductPerformance>> GetSlowMoversAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductPerformance>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);
    Task AddAsync(ProductPerformance performance, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductPerformance performance, CancellationToken cancellationToken = default);
}
