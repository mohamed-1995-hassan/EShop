namespace EShop.Infrastructure.Idempotency;

/// <summary>
/// API-LEVEL IDEMPOTENCY SERVICE
/// 
/// Ensures that duplicate API requests (same idempotency key) return the same result.
/// 
/// USE CASES:
/// - Payment endpoints (prevent double charge)
/// - Order creation (prevent duplicate orders)
/// - Any mutating operation that shouldn't be repeated
/// 
/// FLOW:
/// 1. Client sends request with Idempotency-Key header
/// 2. Service checks if key was processed before
/// 3. If yes, return cached response
/// 4. If no, process request and cache response
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Try to execute an operation idempotently
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="idempotencyKey">Unique key from client</param>
    /// <param name="operation">The operation to execute</param>
    /// <param name="ttl">How long to remember the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with response and whether it was cached</returns>
    Task<IdempotentResult<T>> ExecuteAsync<T>(
        string idempotencyKey,
        Func<Task<T>> operation,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Check if an idempotency key has been used
    /// </summary>
    Task<bool> ExistsAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the cached response for an idempotency key
    /// </summary>
    Task<T?> GetResponseAsync<T>(
        string idempotencyKey,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Invalidate an idempotency key (force re-execution)
    /// </summary>
    Task InvalidateAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of idempotent execution
/// </summary>
public class IdempotentResult<T>
{
    /// <summary>
    /// The response (cached or newly computed)
    /// </summary>
    public T Response { get; init; } = default!;

    /// <summary>
    /// Whether the response was retrieved from cache
    /// </summary>
    public bool WasCached { get; init; }

    /// <summary>
    /// The idempotency key used
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// When the original request was processed
    /// </summary>
    public DateTime ProcessedAt { get; init; }

    public static IdempotentResult<T> FromCache(T response, string key, DateTime processedAt) => new()
    {
        Response = response,
        WasCached = true,
        IdempotencyKey = key,
        ProcessedAt = processedAt
    };

    public static IdempotentResult<T> Fresh(T response, string key) => new()
    {
        Response = response,
        WasCached = false,
        IdempotencyKey = key,
        ProcessedAt = DateTime.UtcNow
    };
}

/// <summary>
/// Stored idempotency record
/// </summary>
public class IdempotencyRecord
{
    public string Key { get; init; } = string.Empty;
    public string ResponseType { get; init; } = string.Empty;
    public string ResponseJson { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public DateTime ProcessedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? RequestHash { get; init; }
}
