using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Caching;

/// <summary>
/// CACHE WARMUP SERVICE
/// 
/// Pre-populates cache on application startup with:
/// - Frequently accessed data
/// - Static/slow-changing data
/// - Configuration data
/// 
/// Benefits:
/// - Eliminates cold-start latency
/// - Reduces initial database load
/// - Better user experience
/// </summary>
public class CacheWarmupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheWarmupService> _logger;

    public CacheWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<CacheWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting cache warmup...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // Run warmup tasks in parallel
            var warmupTasks = new List<Task>
            {
                WarmupCategoriesAsync(scope, cache, stoppingToken),
                WarmupPopularProductsAsync(scope, cache, stoppingToken),
                WarmupSystemSettingsAsync(scope, cache, stoppingToken),
                WarmupActivePromotionsAsync(scope, cache, stoppingToken)
            };

            await Task.WhenAll(warmupTasks);

            _logger.LogInformation("Cache warmup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warmup failed");
        }
    }

    private async Task WarmupCategoriesAsync(
        IServiceScope scope, 
        ICacheService cache, 
        CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Warming up categories cache...");
            
            // Categories are relatively static, warm them up
            // In a real app, you'd fetch from repository
            var categories = new List<string> { "Electronics", "Clothing", "Books", "Home", "Sports" };
            
            await cache.SetAsync(
                CacheKeysExtended.CategoryTree(),
                categories,
                CacheOptions.LongLived,
                ct);

            _logger.LogDebug("Categories cache warmed up");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warmup categories cache");
        }
    }

    private async Task WarmupPopularProductsAsync(
        IServiceScope scope, 
        ICacheService cache, 
        CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Warming up popular products cache...");
            
            // Would fetch top products from repository
            // Placeholder for demo
            await Task.Delay(100, ct);

            _logger.LogDebug("Popular products cache warmed up");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warmup popular products cache");
        }
    }

    private async Task WarmupSystemSettingsAsync(
        IServiceScope scope, 
        ICacheService cache, 
        CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Warming up system settings cache...");
            
            // Would fetch system settings
            var settings = new Dictionary<string, object>
            {
                ["DefaultCurrency"] = "KWD",
                ["TaxEnabled"] = true,
                ["ShippingEnabled"] = true
            };

            await cache.SetAsync(
                CacheKeysExtended.SystemSettings(),
                settings,
                CacheOptions.LongLived,
                ct);

            _logger.LogDebug("System settings cache warmed up");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warmup system settings cache");
        }
    }

    private async Task WarmupActivePromotionsAsync(
        IServiceScope scope, 
        ICacheService cache, 
        CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Warming up active promotions cache...");
            
            // Would fetch active promotions
            await Task.Delay(100, ct);

            _logger.LogDebug("Active promotions cache warmed up");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warmup active promotions cache");
        }
    }
}

/// <summary>
/// Extensions for registering caching services
/// </summary>
public static class CachingServiceExtensions
{
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        // Core cache service (already registered in DependencyInjection)
        // services.AddScoped<ICacheService, RedisCacheService>();
        
        // Cache invalidation service
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        
        // Cache warmup on startup
        services.AddHostedService<CacheWarmupService>();
        
        // Output cache invalidation service (if using output cache)
        services.AddSingleton<OutputCacheInvalidationService>();
        
        return services;
    }

    /// <summary>
    /// Decorator registration for cached repositories
    /// </summary>
    public static IServiceCollection AddCachedRepository<TInterface, TImplementation, TCached>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
        where TCached : class, TInterface
    {
        // Register the actual implementation
        services.AddScoped<TImplementation>();
        
        // Register the cached decorator as the interface
        services.AddScoped<TInterface>(sp =>
        {
            var inner = sp.GetRequiredService<TImplementation>();
            var cache = sp.GetRequiredService<ICacheService>();
            var logger = sp.GetRequiredService<ILogger<TCached>>();
            
            return (TInterface)Activator.CreateInstance(typeof(TCached), inner, cache, logger)!;
        });

        return services;
    }
}

/// <summary>
/// Output cache invalidation service placeholder
/// (Actual implementation in API project)
/// </summary>
public class OutputCacheInvalidationService
{
    // Placeholder - actual implementation in API project
    public virtual ValueTask InvalidateProductsAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
    public virtual ValueTask InvalidateCategoriesAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
    public virtual ValueTask InvalidateContentAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
}
