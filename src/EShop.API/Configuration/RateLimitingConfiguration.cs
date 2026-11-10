using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EShop.API.Configuration;

/// <summary>
/// API RATE LIMITING CONFIGURATION
/// 
/// Protects the API from:
/// - DDoS attacks
/// - Brute force attempts
/// - Resource exhaustion
/// - API abuse
/// 
/// Uses ASP.NET Core 8 built-in rate limiting with multiple policies.
/// </summary>
public static class RateLimitingConfiguration
{
    public const string FixedPolicy = "fixed";
    public const string SlidingPolicy = "sliding";
    public const string TokenBucketPolicy = "token-bucket";
    public const string ConcurrencyPolicy = "concurrency";
    
    // Specialized policies
    public const string AuthPolicy = "auth";
    public const string PaymentPolicy = "payment";
    public const string SearchPolicy = "search";
    public const string UploadPolicy = "upload";

    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() 
            ?? new RateLimitingSettings();

        services.AddRateLimiter(options =>
        {
            // Global rejection handler
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers["Retry-After"] = 
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? ((int)retryAfter.TotalSeconds).ToString()
                        : "60";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) 
                        ? (int)retry.TotalSeconds 
                        : 60
                }, cancellationToken);
            };

            // ═══════════════════════════════════════════════════════════════
            // FIXED WINDOW RATE LIMITER
            // Simple: X requests per time window, resets at interval
            // Good for: General API protection
            // ═══════════════════════════════════════════════════════════════
            options.AddFixedWindowLimiter(FixedPolicy, opt =>
            {
                opt.PermitLimit = settings.FixedWindow.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(settings.FixedWindow.WindowSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.FixedWindow.QueueLimit;
            });

            // ═══════════════════════════════════════════════════════════════
            // SLIDING WINDOW RATE LIMITER
            // Smoother distribution, prevents burst at window boundaries
            // Good for: API endpoints needing smooth traffic
            // ═══════════════════════════════════════════════════════════════
            options.AddSlidingWindowLimiter(SlidingPolicy, opt =>
            {
                opt.PermitLimit = settings.SlidingWindow.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(settings.SlidingWindow.WindowSeconds);
                opt.SegmentsPerWindow = settings.SlidingWindow.SegmentsPerWindow;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.SlidingWindow.QueueLimit;
            });

            // ═══════════════════════════════════════════════════════════════
            // TOKEN BUCKET RATE LIMITER
            // Allows bursts up to bucket size, then rate-limited
            // Good for: APIs with bursty but overall limited traffic
            // ═══════════════════════════════════════════════════════════════
            options.AddTokenBucketLimiter(TokenBucketPolicy, opt =>
            {
                opt.TokenLimit = settings.TokenBucket.TokenLimit;
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(settings.TokenBucket.ReplenishmentSeconds);
                opt.TokensPerPeriod = settings.TokenBucket.TokensPerPeriod;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.TokenBucket.QueueLimit;
                opt.AutoReplenishment = true;
            });

            // ═══════════════════════════════════════════════════════════════
            // CONCURRENCY LIMITER
            // Limits simultaneous requests, not rate over time
            // Good for: Resource-intensive endpoints
            // ═══════════════════════════════════════════════════════════════
            options.AddConcurrencyLimiter(ConcurrencyPolicy, opt =>
            {
                opt.PermitLimit = settings.Concurrency.PermitLimit;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.Concurrency.QueueLimit;
            });

            // ═══════════════════════════════════════════════════════════════
            // SPECIALIZED POLICIES
            // ═══════════════════════════════════════════════════════════════

            // AUTH: Strict limit for login/register (brute force protection)
            options.AddFixedWindowLimiter(AuthPolicy, opt =>
            {
                opt.PermitLimit = 10;      // 10 attempts
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;        // No queuing for auth
            });

            // PAYMENT: Very strict limit (fraud protection)
            options.AddFixedWindowLimiter(PaymentPolicy, opt =>
            {
                opt.PermitLimit = 5;       // 5 payment attempts
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            // SEARCH: Higher limit with burst support
            options.AddTokenBucketLimiter(SearchPolicy, opt =>
            {
                opt.TokenLimit = 30;       // Burst of 30
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
                opt.TokensPerPeriod = 5;   // 5 per second sustained
                opt.QueueLimit = 10;
                opt.AutoReplenishment = true;
            });

            // UPLOAD: Concurrency limited (resource intensive)
            options.AddConcurrencyLimiter(UploadPolicy, opt =>
            {
                opt.PermitLimit = 5;       // 5 concurrent uploads
                opt.QueueLimit = 10;
            });

            // ═══════════════════════════════════════════════════════════════
            // USER-SPECIFIC RATE LIMITING (by user ID or IP)
            // ═══════════════════════════════════════════════════════════════
            options.AddPolicy("per-user", httpContext =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 5
                    });
            });

            // IP-based rate limiting
            options.AddPolicy("per-ip", httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: ip,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        QueueLimit = 10
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}

/// <summary>
/// Rate limiting settings from configuration
/// </summary>
public class RateLimitingSettings
{
    public FixedWindowSettings FixedWindow { get; set; } = new();
    public SlidingWindowSettings SlidingWindow { get; set; } = new();
    public TokenBucketSettings TokenBucket { get; set; } = new();
    public ConcurrencySettings Concurrency { get; set; } = new();
}

public class FixedWindowSettings
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 10;
}

public class SlidingWindowSettings
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int SegmentsPerWindow { get; set; } = 4;
    public int QueueLimit { get; set; } = 10;
}

public class TokenBucketSettings
{
    public int TokenLimit { get; set; } = 50;
    public int ReplenishmentSeconds { get; set; } = 1;
    public int TokensPerPeriod { get; set; } = 10;
    public int QueueLimit { get; set; } = 10;
}

public class ConcurrencySettings
{
    public int PermitLimit { get; set; } = 20;
    public int QueueLimit { get; set; } = 10;
}
