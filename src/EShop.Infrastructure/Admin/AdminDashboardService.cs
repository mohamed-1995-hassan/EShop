using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Admin;

/// <summary>
/// Implementation of admin dashboard service
/// </summary>
public class AdminDashboardService : IAdminDashboardService
{
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(EShopDbContext dbContext, ILogger<AdminDashboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        // Current month metrics
        var currentMonthOrders = await _dbContext.Orders
            .Where(o => o.OrderDate >= startOfMonth)
            .ToListAsync(cancellationToken);

        var lastMonthOrders = await _dbContext.Orders
            .Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate < startOfMonth)
            .ToListAsync(cancellationToken);

        var totalRevenue = currentMonthOrders.Sum(o => o.TotalAmount.Amount);
        var lastMonthRevenue = lastMonthOrders.Sum(o => o.TotalAmount.Amount);
        var revenueChange = lastMonthRevenue > 0 ? ((totalRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 : 0;

        var totalOrders = currentMonthOrders.Count;
        var lastMonthOrderCount = lastMonthOrders.Count;
        var ordersChange = lastMonthOrderCount > 0 ? ((decimal)(totalOrders - lastMonthOrderCount) / lastMonthOrderCount) * 100 : 0;

        var totalCustomers = await _dbContext.Customers.CountAsync(cancellationToken);
        var customersLastMonth = await _dbContext.Customers
            .CountAsync(c => c.CreatedAt < startOfMonth, cancellationToken);
        var customersChange = customersLastMonth > 0 ? ((decimal)(totalCustomers - customersLastMonth) / customersLastMonth) * 100 : 0;

        var totalProducts = await _dbContext.Products.CountAsync(cancellationToken);

        return new DashboardOverviewDto
        {
            TotalRevenue = totalRevenue,
            RevenueChange = revenueChange,
            TotalOrders = totalOrders,
            OrdersChange = ordersChange,
            TotalCustomers = totalCustomers,
            CustomersChange = customersChange,
            TotalProducts = totalProducts,
            ProductsChange = 0, // Products don't typically have month-over-month growth tracking
            AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
            ConversionRate = 0 // Would need visitor tracking for actual conversion rate
        };
    }

    public async Task<SalesAnalyticsDto> GetSalesAnalyticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .OrderBy(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        var dailySales = orders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new SalesDataPointDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount.Amount),
                OrderCount = g.Count()
            })
            .ToList();

        var weeklySales = orders
            .GroupBy(o => GetStartOfWeek(o.OrderDate))
            .Select(g => new SalesDataPointDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount.Amount),
                OrderCount = g.Count()
            })
            .ToList();

        var monthlySales = orders
            .GroupBy(o => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1))
            .Select(g => new SalesDataPointDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount.Amount),
                OrderCount = g.Count()
            })
            .ToList();

        var totalRevenue = orders.Sum(o => o.TotalAmount.Amount);
        var totalOrders = orders.Count;

        return new SalesAnalyticsDto
        {
            DailySales = dailySales,
            WeeklySales = weeklySales,
            MonthlySales = monthlySales,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0
        };
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .Include(o => o.OrderLines)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        // Get order line items grouped by product
        var topProducts = await query
            .SelectMany(o => o.OrderLines)
            .GroupBy(li => li.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(li => li.Quantity),
                Revenue = g.Sum(li => li.LineTotal.Amount)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(count)
            .ToListAsync(cancellationToken);

        var productIds = topProducts.Select(p => p.ProductId).ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return topProducts.Select(tp => new TopProductDto
        {
            ProductId = tp.ProductId,
            Name = products.TryGetValue(tp.ProductId, out var product) ? product.Name : "Unknown",
            Sku = product?.Sku ?? "N/A",
            QuantitySold = tp.QuantitySold,
            Revenue = tp.Revenue,
            ImageUrl = string.Empty // Would need image storage implementation
        }).ToList();
    }

    public async Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var recentOrders = await _dbContext.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .ToListAsync(cancellationToken);

        return recentOrders.Select(o => new RecentOrderDto
        {
            OrderId = o.Id,
            OrderNumber = o.Id.ToString().Substring(0, 8).ToUpper(),
            CustomerName = "Customer", // Customer not loaded in this query
            Total = o.TotalAmount.Amount,
            Status = o.Status.ToString(),
            CreatedAt = o.OrderDate
        }).ToList();
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would have an activity log table
        // For now, we'll generate activity from recent orders
        var recentOrders = await _dbContext.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .ToListAsync(cancellationToken);

        return recentOrders.Select(o => new RecentActivityDto
        {
            ActivityId = Guid.NewGuid().ToString(),
            Type = "OrderCreated",
            Description = $"Order {o.Id.ToString().Substring(0, 8).ToUpper()} was placed",
            EntityType = "Order",
            EntityId = o.Id.ToString(),
            PerformedBy = "Customer",
            Timestamp = o.OrderDate
        }).ToList();
    }

    public async Task<IReadOnlyList<InventoryAlertDto>> GetInventoryAlertsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .Where(p => p.StockQuantity <= p.LowStockThreshold || p.StockQuantity >= 1000)
            .ToListAsync(cancellationToken);

        return products.Select(p =>
        {
            var alertType = p.StockQuantity == 0 ? "OutOfStock" :
                           p.StockQuantity <= p.LowStockThreshold ? "LowStock" : "Overstock";
            var severity = p.StockQuantity == 0 ? "Critical" :
                          p.StockQuantity <= 5 ? "High" :
                          p.StockQuantity <= p.LowStockThreshold ? "Medium" : "Low";

            return new InventoryAlertDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                CurrentStock = p.StockQuantity,
                ReorderLevel = p.LowStockThreshold,
                AlertType = alertType,
                Severity = severity
            };
        }).ToList();
    }

    public Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would check actual service health
        return Task.FromResult(new SystemHealthDto
        {
            Status = "Healthy",
            CpuUsage = 45.5,
            MemoryUsage = 62.3,
            DiskUsage = 55.0,
            Services = new List<ServiceHealthDto>
            {
                new() { Name = "Database", Status = "Healthy", ResponseTimeMs = 5.2 },
                new() { Name = "Redis Cache", Status = "Healthy", ResponseTimeMs = 1.1 },
                new() { Name = "RabbitMQ", Status = "Healthy", ResponseTimeMs = 2.3 },
                new() { Name = "Stripe API", Status = "Healthy", ResponseTimeMs = 150.5 }
            },
            LastUpdated = DateTime.UtcNow
        });
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
