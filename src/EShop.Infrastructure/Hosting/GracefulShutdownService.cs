using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Hosting;

/// <summary>
/// GRACEFUL SHUTDOWN HANDLER
/// 
/// Ensures clean shutdown when:
/// - SIGTERM received (Kubernetes)
/// - App pool recycled (IIS)
/// - Docker container stopped
/// 
/// SHUTDOWN ORDER:
/// 1. Stop accepting new requests
/// 2. Complete in-flight requests
/// 3. Drain message consumers
/// 4. Flush outbox
/// 5. Close connections
/// </summary>
public class GracefulShutdownService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<GracefulShutdownService> _logger;
    private readonly IServiceProvider _services;
    private readonly TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(30);

    public GracefulShutdownService(
        IHostApplicationLifetime lifetime,
        ILogger<GracefulShutdownService> logger,
        IServiceProvider services)
    {
        _lifetime = lifetime;
        _logger = logger;
        _services = services;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(OnStarted);
        _lifetime.ApplicationStopping.Register(OnStopping);
        _lifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnStarted()
    {
        _logger.LogInformation("Application started. Ready to process requests.");
    }

    private void OnStopping()
    {
        _logger.LogWarning("Application is shutting down. Initiating graceful shutdown...");

        try
        {
            // Give time for load balancer to remove this instance
            _logger.LogInformation("Waiting for load balancer drain period (5s)...");
            Thread.Sleep(TimeSpan.FromSeconds(5));

            // The framework will handle stopping hosted services
            _logger.LogInformation("Graceful shutdown initiated. Waiting for in-flight requests...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during graceful shutdown");
        }
    }

    private void OnStopped()
    {
        _logger.LogInformation("Application stopped gracefully.");
    }
}

/// <summary>
/// Health endpoint for Kubernetes probes
/// </summary>
public static class KubernetesHealthExtensions
{
    private static volatile bool _isReady = false;
    private static volatile bool _isHealthy = true;

    /// <summary>
    /// Mark application as ready (for readiness probe)
    /// </summary>
    public static void MarkReady() => _isReady = true;

    /// <summary>
    /// Mark application as not ready (during shutdown)
    /// </summary>
    public static void MarkNotReady() => _isReady = false;

    /// <summary>
    /// Mark application as unhealthy (for liveness probe)
    /// </summary>
    public static void MarkUnhealthy() => _isHealthy = false;

    public static bool IsReady => _isReady;
    public static bool IsHealthy => _isHealthy;
}
