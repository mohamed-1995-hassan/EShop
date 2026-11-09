using EShop.Domain.Aggregates.AnalyticsAggregate;
using EShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly ISalesReportRepository _salesReportRepo;
    private readonly ICustomerInsightRepository _customerInsightRepo;
    private readonly IProductPerformanceRepository _productPerformanceRepo;

    public AnalyticsController(
        ISalesReportRepository salesReportRepo,
        ICustomerInsightRepository customerInsightRepo,
        IProductPerformanceRepository productPerformanceRepo)
    {
        _salesReportRepo = salesReportRepo;
        _customerInsightRepo = customerInsightRepo;
        _productPerformanceRepo = productPerformanceRepo;
    }

    // Sales Reports
    [HttpGet("sales/{id:guid}")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(Guid id)
    {
        var report = await _salesReportRepo.GetByIdAsync(id);
        return report != null ? Ok(MapToDto(report)) : NotFound();
    }

    [HttpGet("sales/recent")]
    public async Task<ActionResult<IReadOnlyList<SalesReportDto>>> GetRecentSalesReports([FromQuery] int count = 10)
    {
        var reports = await _salesReportRepo.GetRecentAsync(count);
        return Ok(reports.Select(MapToDto).ToList());
    }

    [HttpPost("sales/create")]
    public async Task<ActionResult<Guid>> CreateSalesReport([FromBody] CreateSalesReportRequest request)
    {
        var report = SalesReport.Create(request.Period, request.StartDate, request.EndDate, request.Currency);
        await _salesReportRepo.AddAsync(report);
        return Ok(report.Id);
    }

    [HttpPost("sales/{id:guid}/complete")]
    public async Task<IActionResult> CompleteSalesReport(Guid id)
    {
        var report = await _salesReportRepo.GetByIdAsync(id);
        if (report == null) return NotFound();

        report.Complete();
        await _salesReportRepo.UpdateAsync(report);
        return NoContent();
    }

    // Customer Insights
    [HttpGet("customers/{id:guid}")]
    public async Task<ActionResult<CustomerInsightDto>> GetCustomerInsight(Guid id)
    {
        var insight = await _customerInsightRepo.GetByIdAsync(id);
        return insight != null ? Ok(MapToDto(insight)) : NotFound();
    }

    [HttpGet("customers/by-customer/{customerId:guid}")]
    public async Task<ActionResult<CustomerInsightDto>> GetByCustomerId(Guid customerId)
    {
        var insight = await _customerInsightRepo.GetByCustomerIdAsync(customerId);
        return insight != null ? Ok(MapToDto(insight)) : NotFound();
    }

    [HttpGet("customers/segment/{segment}")]
    public async Task<ActionResult<IReadOnlyList<CustomerInsightDto>>> GetBySegment(CustomerSegment segment)
    {
        var insights = await _customerInsightRepo.GetBySegmentAsync(segment);
        return Ok(insights.Select(MapToDto).ToList());
    }

    [HttpGet("customers/at-risk")]
    public async Task<ActionResult<IReadOnlyList<CustomerInsightDto>>> GetAtRiskCustomers()
    {
        var insights = await _customerInsightRepo.GetAtRiskCustomersAsync();
        return Ok(insights.Select(MapToDto).ToList());
    }

    [HttpGet("customers/top")]
    public async Task<ActionResult<IReadOnlyList<CustomerInsightDto>>> GetTopCustomers([FromQuery] int count = 10)
    {
        var insights = await _customerInsightRepo.GetTopCustomersAsync(count);
        return Ok(insights.Select(MapToDto).ToList());
    }

    [HttpPost("customers/create")]
    public async Task<ActionResult<Guid>> CreateCustomerInsight([FromBody] CreateCustomerInsightRequest request)
    {
        var insight = CustomerInsight.Create(request.CustomerId, request.Currency);
        await _customerInsightRepo.AddAsync(insight);
        return Ok(insight.Id);
    }

    // Product Performance
    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<ProductPerformanceDto>> GetProductPerformance(Guid id)
    {
        var performance = await _productPerformanceRepo.GetByIdAsync(id);
        return performance != null ? Ok(MapToDto(performance)) : NotFound();
    }

    [HttpGet("products/by-product/{productId:guid}")]
    public async Task<ActionResult<ProductPerformanceDto>> GetByProductId(Guid productId)
    {
        var performance = await _productPerformanceRepo.GetByProductIdAsync(productId);
        return performance != null ? Ok(MapToDto(performance)) : NotFound();
    }

    [HttpGet("products/top-sellers")]
    public async Task<ActionResult<IReadOnlyList<ProductPerformanceDto>>> GetTopSellers([FromQuery] int count = 10)
    {
        var performances = await _productPerformanceRepo.GetTopSellersAsync(count);
        return Ok(performances.Select(MapToDto).ToList());
    }

    [HttpGet("products/slow-movers")]
    public async Task<ActionResult<IReadOnlyList<ProductPerformanceDto>>> GetSlowMovers([FromQuery] int count = 10)
    {
        var performances = await _productPerformanceRepo.GetSlowMoversAsync(count);
        return Ok(performances.Select(MapToDto).ToList());
    }

    [HttpGet("products/low-stock")]
    public async Task<ActionResult<IReadOnlyList<ProductPerformanceDto>>> GetLowStock([FromQuery] int threshold = 10)
    {
        var performances = await _productPerformanceRepo.GetLowStockAsync(threshold);
        return Ok(performances.Select(MapToDto).ToList());
    }

    [HttpPost("products/create")]
    public async Task<ActionResult<Guid>> CreateProductPerformance([FromBody] CreateProductPerformanceRequest request)
    {
        var performance = ProductPerformance.Create(request.ProductId, request.Currency);
        await _productPerformanceRepo.AddAsync(performance);
        return Ok(performance.Id);
    }

    [HttpPost("products/{id:guid}/view")]
    public async Task<IActionResult> RecordProductView(Guid id)
    {
        var performance = await _productPerformanceRepo.GetByIdAsync(id);
        if (performance == null) return NotFound();

        performance.RecordView();
        await _productPerformanceRepo.UpdateAsync(performance);
        return NoContent();
    }

    [HttpPost("products/{id:guid}/add-to-cart")]
    public async Task<IActionResult> RecordAddToCart(Guid id)
    {
        var performance = await _productPerformanceRepo.GetByIdAsync(id);
        if (performance == null) return NotFound();

        performance.RecordAddToCart();
        await _productPerformanceRepo.UpdateAsync(performance);
        return NoContent();
    }

    [HttpPost("products/{id:guid}/purchase")]
    public async Task<IActionResult> RecordProductPurchase(Guid id, [FromBody] RecordPurchaseRequest request)
    {
        var performance = await _productPerformanceRepo.GetByIdAsync(id);
        if (performance == null) return NotFound();

        performance.RecordPurchase(new Domain.ValueObjects.Money(request.Amount, request.Currency));
        await _productPerformanceRepo.UpdateAsync(performance);
        return NoContent();
    }

    [HttpPost("products/{id:guid}/return")]
    public async Task<IActionResult> RecordProductReturn(Guid id)
    {
        var performance = await _productPerformanceRepo.GetByIdAsync(id);
        if (performance == null) return NotFound();

        performance.RecordReturn();
        await _productPerformanceRepo.UpdateAsync(performance);
        return NoContent();
    }

    // Mapping helpers
    private static SalesReportDto MapToDto(SalesReport r) =>
        new(r.Id, r.Period.ToString(), r.StartDate, r.EndDate, r.Status.ToString(), r.TotalOrders, r.TotalItems, r.UniqueCustomers, r.TotalRevenue.Amount, r.TotalDiscounts.Amount, r.NetRevenue.Amount, r.AverageOrderValue.Amount, r.ConversionRate, r.TotalRevenue.Currency);

    private static CustomerInsightDto MapToDto(CustomerInsight c) =>
        new(c.Id, c.CustomerId, c.Segment.ToString(), c.TotalOrders, c.LifetimeValue.Amount, c.AverageOrderValue.Amount, c.DaysSinceLastOrder, c.IsAtRisk, c.LifetimeValue.Currency);

    private static ProductPerformanceDto MapToDto(ProductPerformance p) =>
        new(p.Id, p.ProductId, p.TotalViews, p.TotalAddedToCart, p.TotalPurchases, p.TotalReturns, p.ViewToCartRate, p.CartToPurchaseRate, p.TotalRevenue.Amount, p.IsTopSeller, p.IsSlowMover, p.TotalRevenue.Currency);
}

// DTOs
public record SalesReportDto(Guid Id, string Period, DateTime StartDate, DateTime EndDate, string Status, int TotalOrders, int TotalItems, int UniqueCustomers, decimal TotalRevenue, decimal TotalDiscounts, decimal NetRevenue, decimal AverageOrderValue, decimal ConversionRate, string Currency);
public record CustomerInsightDto(Guid Id, Guid CustomerId, string Segment, int TotalOrders, decimal LifetimeValue, decimal AverageOrderValue, int DaysSinceLastOrder, bool IsAtRisk, string Currency);
public record ProductPerformanceDto(Guid Id, Guid ProductId, int TotalViews, int TotalAddedToCart, int TotalPurchases, int TotalReturns, decimal ViewToCartRate, decimal CartToPurchaseRate, decimal TotalRevenue, bool IsTopSeller, bool IsSlowMover, string Currency);

// Requests
public record CreateSalesReportRequest(ReportPeriod Period, DateTime StartDate, DateTime EndDate, string Currency = "USD");
public record CreateCustomerInsightRequest(Guid CustomerId, string Currency = "USD");
public record CreateProductPerformanceRequest(Guid ProductId, string Currency = "USD");
public record RecordPurchaseRequest(decimal Amount, string Currency);
