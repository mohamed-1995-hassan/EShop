namespace EShop.Infrastructure.RateLimiting;

/// <summary>
/// DISTRIBUTED RATE LIMITER
/// 
/// Protects services from:
/// - DDoS attacks
/// - Runaway clients
/// - Resource exhaustion
/// - API abuse
/// 
/// ALGORITHMS:
/// - Fixed Window: Simple, reset at interval
/// - Sliding Window: Smoother distribution
/// - Token Bucket: Burst-friendly
/// - Leaky Bucket: Constant rate
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Check if request is allowed and consume a token
    /// </summary>
    Task<RateLimitResult> CheckAsync(
        string key,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check without consuming (peek)
    /// </summary>
    Task<RateLimitResult> PeekAsync(
        string key,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset rate limit for key
    /// </summary>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current state for key
    /// </summary>
    Task<RateLimitState?> GetStateAsync(
        string key,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limit check result
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; init; }
    public int RemainingTokens { get; init; }
    public int Limit { get; init; }
    public TimeSpan RetryAfter { get; init; }
    public DateTime ResetAt { get; init; }

    public static RateLimitResult Allowed(int remaining, int limit, DateTime resetAt) => new()
    {
        IsAllowed = true,
        RemainingTokens = remaining,
        Limit = limit,
        ResetAt = resetAt
    };

    public static RateLimitResult Denied(int limit, TimeSpan retryAfter, DateTime resetAt) => new()
    {
        IsAllowed = false,
        RemainingTokens = 0,
        Limit = limit,
        RetryAfter = retryAfter,
        ResetAt = resetAt
    };
}

/// <summary>
/// Rate limit policy configuration
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Policy name for identification
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Maximum requests allowed in the window
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Time window duration
    /// </summary>
    public TimeSpan Window { get; init; }

    /// <summary>
    /// Rate limiting algorithm
    /// </summary>
    public RateLimitAlgorithm Algorithm { get; init; } = RateLimitAlgorithm.SlidingWindow;

    /// <summary>
    /// Allow burst above limit (token bucket)
    /// </summary>
    public int? BurstLimit { get; init; }

    // Predefined policies
    public static RateLimitPolicy Default => new()
    {
        Name = "default",
        Limit = 100,
        Window = TimeSpan.FromMinutes(1),
        Algorithm = RateLimitAlgorithm.SlidingWindow
    };

    public static RateLimitPolicy Strict => new()
    {
        Name = "strict",
        Limit = 10,
        Window = TimeSpan.FromMinutes(1),
        Algorithm = RateLimitAlgorithm.FixedWindow
    };

    public static RateLimitPolicy Api => new()
    {
        Name = "api",
        Limit = 1000,
        Window = TimeSpan.FromHours(1),
        Algorithm = RateLimitAlgorithm.SlidingWindow
    };

    public static RateLimitPolicy Payment => new()
    {
        Name = "payment",
        Limit = 5,
        Window = TimeSpan.FromMinutes(1),
        Algorithm = RateLimitAlgorithm.FixedWindow
    };

    public static RateLimitPolicy Search => new()
    {
        Name = "search",
        Limit = 30,
        Window = TimeSpan.FromMinutes(1),
        Algorithm = RateLimitAlgorithm.TokenBucket,
        BurstLimit = 10
    };
}

public enum RateLimitAlgorithm
{
    FixedWindow,
    SlidingWindow,
    TokenBucket,
    LeakyBucket
}

/// <summary>
/// Current rate limit state
/// </summary>
public class RateLimitState
{
    public string Key { get; init; } = string.Empty;
    public int CurrentCount { get; init; }
    public int Limit { get; init; }
    public DateTime WindowStart { get; init; }
    public DateTime WindowEnd { get; init; }
}
