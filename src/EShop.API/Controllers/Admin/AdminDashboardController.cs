using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers.Admin;

/// <summary>
/// Admin Dashboard - Overview and Analytics
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin,Manager")]
[Produces("application/json")]
[Tags("Admin Dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        IAdminDashboardService dashboardService,
        ILogger<AdminDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetOverviewAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get sales analytics
    /// </summary>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(SalesAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesAnalyticsDto>> GetSalesAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var result = await _dashboardService.GetSalesAnalyticsAsync(start, end, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get top selling products
    /// </summary>
    [HttpGet("top-products")]
    [ProducesResponseType(typeof(IReadOnlyList<TopProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TopProductDto>>> GetTopProducts(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetTopProductsAsync(count, startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get recent orders
    /// </summary>
    [HttpGet("recent-orders")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentOrderDto>>> GetRecentOrders(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetRecentOrdersAsync(count, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get recent activity
    /// </summary>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentActivityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentActivityDto>>> GetRecentActivity(
        [FromQuery] int count = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetRecentActivityAsync(count, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get inventory alerts (low stock, out of stock)
    /// </summary>
    [HttpGet("inventory-alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InventoryAlertDto>>> GetInventoryAlerts(
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetInventoryAlertsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth(
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetSystemHealthAsync(cancellationToken);
        return Ok(result);
    }
}
