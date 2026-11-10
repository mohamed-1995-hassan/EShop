using EShop.Application.Interfaces.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Infrastructure.Admin;

/// <summary>
/// Dependency injection extensions for admin services
/// </summary>
public static class AdminDependencyInjection
{
    /// <summary>
    /// Add admin dashboard and management services
    /// </summary>
    public static IServiceCollection AddAdminServices(this IServiceCollection services)
    {
        // Dashboard service
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        // Management services
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminProductService, AdminProductService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminSettingsService, AdminSettingsService>();

        return services;
    }
}
