using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace EShop.Infrastructure.Resilience;

/// <summary>
/// Resilience policies using Polly for:
/// - Retries with exponential backoff
/// - Circuit breaker for failing services
/// - Timeout for long-running operations
/// - Bulkhead for limiting concurrent calls
/// 
/// RESILIENCE PATTERNS:
/// 1. Retry: Automatically retry failed operations with backoff
/// 2. Circuit Breaker: Stop calling failing services to let them recover
/// 3. Timeout: Cancel long-running operations
/// 4. Bulkhead: Limit concurrent operations to prevent overload
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Standard retry policy with exponential backoff
    /// Retries: 1s, 2s, 4s, 8s, 16s
    /// </summary>
    public static ResiliencePipeline<T> CreateStandardRetryPipeline<T>()
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true, // Adds randomness to prevent thundering herd
                OnRetry = args =>
                {
                    Console.WriteLine($"Retry attempt {args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds}ms");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Circuit breaker policy
    /// Opens after 5 failures, stays open for 30 seconds
    /// </summary>
    public static ResiliencePipeline<T> CreateCircuitBreakerPipeline<T>()
    {
        return new ResiliencePipelineBuilder<T>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5, // 50% failure rate triggers breaker
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10, // Minimum calls before calculating ratio
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    Console.WriteLine($"Circuit opened due to: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("Circuit closed, resuming normal operations");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("Circuit half-open, testing...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Timeout policy - 30 second timeout
    /// </summary>
    public static ResiliencePipeline<T> CreateTimeoutPipeline<T>(TimeSpan timeout)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeout,
                OnTimeout = args =>
                {
                    Console.WriteLine($"Operation timed out after {args.Timeout.TotalSeconds}s");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Combined resilience pipeline: Retry -> Circuit Breaker -> Timeout
    /// Order matters: Timeout wraps Circuit Breaker wraps Retry
    /// </summary>
    public static ResiliencePipeline<T> CreateComprehensivePipeline<T>(
        int maxRetries = 3,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);

        return new ResiliencePipelineBuilder<T>()
            // Outer: Timeout
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeout.Value
            })
            // Middle: Circuit Breaker
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            // Inner: Retry
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = maxRetries,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true
            })
            .Build();
    }
}

/// <summary>
/// Named resilience pipelines for different scenarios
/// </summary>
public static class ResiliencePipelineNames
{
    public const string Database = "Database";
    public const string HttpClient = "HttpClient";
    public const string MessageBroker = "MessageBroker";
    public const string PaymentGateway = "PaymentGateway";
    public const string ExternalApi = "ExternalApi";
}

/// <summary>
/// Extension methods for registering resilience pipelines
/// </summary>
public static class ResilienceExtensions
{
    public static IServiceCollection AddResiliencePipelines(this IServiceCollection services)
    {
        services.AddResiliencePipeline(ResiliencePipelineNames.Database, builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMilliseconds(500),
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<TimeoutException>()
                        .Handle<InvalidOperationException>(ex => 
                            ex.Message.Contains("connection"))
                })
                .AddTimeout(TimeSpan.FromSeconds(30));
        });

        services.AddResiliencePipeline(ResiliencePipelineNames.HttpClient, builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(1),
                    UseJitter = true
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(30)
                })
                .AddTimeout(TimeSpan.FromSeconds(10));
        });

        services.AddResiliencePipeline(ResiliencePipelineNames.PaymentGateway, builder =>
        {
            // Payment is critical - more retries, longer timeout
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 5,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromMinutes(1),
                    UseJitter = true
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.3, // More sensitive
                    SamplingDuration = TimeSpan.FromMinutes(1),
                    MinimumThroughput = 20,
                    BreakDuration = TimeSpan.FromMinutes(2)
                })
                .AddTimeout(TimeSpan.FromSeconds(60));
        });

        return services;
    }
}
