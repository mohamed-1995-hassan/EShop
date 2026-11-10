using EShop.Application.DTOs;

namespace EShop.Application.Interfaces.Admin;

/// <summary>
/// Service interface for admin dashboard operations
/// </summary>
public interface IAdminDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<SalesAnalyticsDto> GetSalesAnalyticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(int count = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryAlertDto>> GetInventoryAlertsAsync(CancellationToken cancellationToken = default);
    Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);
}
