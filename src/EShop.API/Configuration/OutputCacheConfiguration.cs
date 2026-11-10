using Microsoft.AspNetCore.OutputCaching;

namespace EShop.API.Configuration;

/// <summary>
/// OUTPUT CACHING CONFIGURATION (ASP.NET Core 7+)
/// 
/// Caches HTTP responses at the edge:
/// - Reduces server load for identical requests
/// - Works with CDNs and reverse proxies
/// - Automatic cache invalidation support
/// 
/// Different from distributed cache:
/// - Output cache = HTTP response caching
/// - Distributed cache = Application data caching
/// </summary>
public static class OutputCacheConfiguration
{
    public static IServiceCollection AddOutputCacheConfiguration(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            // Default policy - cache for 60 seconds
            options.AddBasePolicy(builder => 
                builder.Expire(TimeSpan.FromSeconds(60)));

            // Products - cache longer, vary by query params
            options.AddPolicy("Products", builder =>
                builder
                    .Expire(TimeSpan.FromMinutes(5))
                    .SetVaryByQuery("page", "size", "category", "search")
                    .Tag("products"));

            // Product detail - cache with tag for invalidation
            options.AddPolicy("ProductDetail", builder =>
                builder
                    .Expire(TimeSpan.FromMinutes(10))
                    .SetVaryByRouteValue("id")
                    .Tag("products"));

            // Categories - stable data, cache longer
            options.AddPolicy("Categories", builder =>
                builder
                    .Expire(TimeSpan.FromHours(1))
                    .Tag("categories"));

            // Search results - shorter cache, many variations
            options.AddPolicy("Search", builder =>
                builder
                    .Expire(TimeSpan.FromMinutes(2))
                    .SetVaryByQuery("q", "page", "size", "sort"));

            // Public content (FAQ, Blog) - long cache
            options.AddPolicy("Content", builder =>
                builder
                    .Expire(TimeSpan.FromHours(24))
                    .Tag("content"));

            // No cache policy for dynamic/user-specific data
            options.AddPolicy("NoCache", builder =>
                builder.NoCache());

            // Authenticated user data - don't cache
            options.AddPolicy("Authenticated", builder =>
                builder
                    .Expire(TimeSpan.FromSeconds(30))
                    .SetVaryByHeader("Authorization"));
        });

        return services;
    }

    public static IApplicationBuilder UseOutputCacheMiddleware(this IApplicationBuilder app)
    {
        app.UseOutputCache();
        return app;
    }
}

/// <summary>
/// Cache invalidation service for output cache
/// </summary>
public class OutputCacheInvalidationService
{
    private readonly IOutputCacheStore _store;

    public OutputCacheInvalidationService(IOutputCacheStore store)
    {
        _store = store;
    }

    public async ValueTask InvalidateProductsAsync(CancellationToken cancellationToken = default)
    {
        await _store.EvictByTagAsync("products", cancellationToken);
    }

    public async ValueTask InvalidateCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await _store.EvictByTagAsync("categories", cancellationToken);
    }

    public async ValueTask InvalidateContentAsync(CancellationToken cancellationToken = default)
    {
        await _store.EvictByTagAsync("content", cancellationToken);
    }

    public async ValueTask InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        await InvalidateProductsAsync(cancellationToken);
        await InvalidateCategoriesAsync(cancellationToken);
        await InvalidateContentAsync(cancellationToken);
    }
}
