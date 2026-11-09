namespace EShop.Infrastructure.DistributedLocking;

/// <summary>
/// DISTRIBUTED LOCKING SERVICE
/// 
/// Provides distributed mutex/lock functionality for:
/// - Preventing duplicate order processing across instances
/// - Ensuring single saga instance execution
/// - Protecting critical sections in distributed systems
/// 
/// PRODUCTION USE CASES:
/// - Payment processing (prevent double charge)
/// - Inventory reservation (prevent overselling)
/// - Outbox processing (prevent duplicate publishing)
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Acquire a distributed lock with automatic expiry
    /// </summary>
    /// <param name="resource">Unique resource identifier (e.g., "order:123")</param>
    /// <param name="expiry">Lock expiration time (prevents deadlocks)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lock handle if acquired, null if lock is held by another process</returns>
    Task<IDistributedLock?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire a distributed lock with retry
    /// </summary>
    /// <param name="resource">Unique resource identifier</param>
    /// <param name="expiry">Lock expiration time</param>
    /// <param name="wait">Maximum time to wait for lock</param>
    /// <param name="retry">Time between retry attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IDistributedLock?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        TimeSpan wait,
        TimeSpan retry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute action with distributed lock protection
    /// </summary>
    Task<T> ExecuteWithLockAsync<T>(
        string resource,
        TimeSpan expiry,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute action with distributed lock protection (no return value)
    /// </summary>
    Task ExecuteWithLockAsync(
        string resource,
        TimeSpan expiry,
        Func<Task> action,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Distributed lock handle - must be disposed to release the lock
/// </summary>
public interface IDistributedLock : IAsyncDisposable, IDisposable
{
    string Resource { get; }
    string LockId { get; }
    bool IsAcquired { get; }
    DateTime? ExpiresAt { get; }

    /// <summary>
    /// Extend the lock expiration
    /// </summary>
    Task<bool> ExtendAsync(TimeSpan extension);
}

/// <summary>
/// Exception thrown when lock acquisition fails
/// </summary>
public class DistributedLockException : Exception
{
    public string Resource { get; }
    public TimeSpan Timeout { get; }

    public DistributedLockException(string resource, TimeSpan timeout)
        : base($"Failed to acquire lock on resource '{resource}' within {timeout.TotalSeconds}s")
    {
        Resource = resource;
        Timeout = timeout;
    }
}
