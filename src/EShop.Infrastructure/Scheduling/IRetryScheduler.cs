namespace EShop.Infrastructure.Scheduling;

/// <summary>
/// RETRY SCHEDULER
/// 
/// Schedules failed operations for future retry with:
/// - Exponential backoff
/// - Maximum retry limits
/// - Jitter to prevent thundering herd
/// 
/// USE CASES:
/// - Failed payment retries
/// - External API call retries
/// - Event publishing retries
/// </summary>
public interface IRetryScheduler
{
    /// <summary>
    /// Schedule an operation for future retry
    /// </summary>
    Task<Guid> ScheduleRetryAsync<T>(
        T payload,
        string operationType,
        DateTime executeAt,
        RetryPolicy? policy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule immediate retry (for manual triggers)
    /// </summary>
    Task<Guid> ScheduleImmediateRetryAsync<T>(
        T payload,
        string operationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a scheduled retry
    /// </summary>
    Task<bool> CancelRetryAsync(Guid retryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending retries for an operation type
    /// </summary>
    Task<IReadOnlyList<ScheduledRetry>> GetPendingRetriesAsync(
        string? operationType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark retry as completed
    /// </summary>
    Task MarkCompletedAsync(Guid retryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark retry as failed and reschedule
    /// </summary>
    Task MarkFailedAndRescheduleAsync(
        Guid retryId,
        string error,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Scheduled retry record
/// </summary>
public record ScheduledRetry
{
    public Guid Id { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public string PayloadType { get; init; } = string.Empty;
    public DateTime ScheduledAt { get; init; }
    public DateTime? ExecutedAt { get; init; }
    public int AttemptCount { get; init; }
    public int MaxAttempts { get; init; }
    public string? LastError { get; init; }
    public RetryStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public enum RetryStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    MaxAttemptsReached
}

/// <summary>
/// Retry policy configuration
/// </summary>
public class RetryPolicy
{
    public int MaxAttempts { get; init; } = 5;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromHours(1);
    public double BackoffMultiplier { get; init; } = 2.0;
    public bool UseJitter { get; init; } = true;
    public double JitterFactor { get; init; } = 0.1; // ±10%

    public static RetryPolicy Default => new();

    public static RetryPolicy Aggressive => new()
    {
        MaxAttempts = 10,
        InitialDelay = TimeSpan.FromSeconds(30),
        MaxDelay = TimeSpan.FromMinutes(30),
        BackoffMultiplier = 1.5
    };

    public static RetryPolicy Conservative => new()
    {
        MaxAttempts = 3,
        InitialDelay = TimeSpan.FromMinutes(5),
        MaxDelay = TimeSpan.FromHours(2),
        BackoffMultiplier = 3.0
    };

    /// <summary>
    /// Calculate next retry delay based on attempt count
    /// </summary>
    public TimeSpan CalculateDelay(int attemptCount)
    {
        var delay = InitialDelay * Math.Pow(BackoffMultiplier, attemptCount - 1);
        
        if (delay > MaxDelay)
            delay = MaxDelay;

        if (UseJitter)
        {
            var jitter = delay * JitterFactor;
            var random = new Random();
            delay += TimeSpan.FromMilliseconds(
                (random.NextDouble() * 2 - 1) * jitter.TotalMilliseconds);
        }

        return delay;
    }
}
