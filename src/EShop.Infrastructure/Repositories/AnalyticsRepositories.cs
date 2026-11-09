using EShop.Domain.Aggregates.AnalyticsAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

public class SalesReportRepository : ISalesReportRepository
{
    private static readonly ConcurrentDictionary<Guid, SalesReport> _reports = new();

    public Task<SalesReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_reports.GetValueOrDefault(id));

    public Task<SalesReport?> GetByPeriodAsync(ReportPeriod period, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        => Task.FromResult(_reports.Values.FirstOrDefault(r => r.Period == period && r.StartDate == startDate && r.EndDate == endDate));

    public Task<IReadOnlyList<SalesReport>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SalesReport>>(_reports.Values.Where(r => r.Status == ReportStatus.Completed).OrderByDescending(r => r.GeneratedAt).Take(count).ToList());

    public Task AddAsync(SalesReport report, CancellationToken cancellationToken = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SalesReport report, CancellationToken cancellationToken = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }
}

public class CustomerInsightRepository : ICustomerInsightRepository
{
    private static readonly ConcurrentDictionary<Guid, CustomerInsight> _insights = new();

    public Task<CustomerInsight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_insights.GetValueOrDefault(id));

    public Task<CustomerInsight?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => Task.FromResult(_insights.Values.FirstOrDefault(i => i.CustomerId == customerId));

    public Task<IReadOnlyList<CustomerInsight>> GetBySegmentAsync(CustomerSegment segment, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CustomerInsight>>(_insights.Values.Where(i => i.Segment == segment).ToList());

    public Task<IReadOnlyList<CustomerInsight>> GetAtRiskCustomersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CustomerInsight>>(_insights.Values.Where(i => i.IsAtRisk).ToList());

    public Task<IReadOnlyList<CustomerInsight>> GetTopCustomersAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CustomerInsight>>(_insights.Values.OrderByDescending(i => i.LifetimeValue.Amount).Take(count).ToList());

    public Task AddAsync(CustomerInsight insight, CancellationToken cancellationToken = default)
    {
        _insights[insight.Id] = insight;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CustomerInsight insight, CancellationToken cancellationToken = default)
    {
        _insights[insight.Id] = insight;
        return Task.CompletedTask;
    }
}

public class ProductPerformanceRepository : IProductPerformanceRepository
{
    private static readonly ConcurrentDictionary<Guid, ProductPerformance> _performances = new();

    public Task<ProductPerformance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_performances.GetValueOrDefault(id));

    public Task<ProductPerformance?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        => Task.FromResult(_performances.Values.FirstOrDefault(p => p.ProductId == productId));

    public Task<IReadOnlyList<ProductPerformance>> GetTopSellersAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProductPerformance>>(_performances.Values.Where(p => p.IsTopSeller).OrderByDescending(p => p.TotalPurchases).Take(count).ToList());

    public Task<IReadOnlyList<ProductPerformance>> GetSlowMoversAsync(int count, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProductPerformance>>(_performances.Values.Where(p => p.IsSlowMover).Take(count).ToList());

    public Task<IReadOnlyList<ProductPerformance>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProductPerformance>>(_performances.Values.Where(p => p.CurrentStock < threshold).ToList());

    public Task AddAsync(ProductPerformance performance, CancellationToken cancellationToken = default)
    {
        _performances[performance.Id] = performance;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ProductPerformance performance, CancellationToken cancellationToken = default)
    {
        _performances[performance.Id] = performance;
        return Task.CompletedTask;
    }
}
