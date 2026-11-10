using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EShop.Infrastructure.Health;

/// <summary>
/// Health check configuration for Kubernetes and monitoring systems.
/// Provides /health/live (liveness) and /health/ready (readiness) endpoints.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Add health checks for all infrastructure components
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Basic health check (always healthy if app is running)
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy("Application is running"),
            tags: new[] { "live" });

        // Database health check
        var dbConnectionString = configuration.GetConnectionString("Database");
        if (!string.IsNullOrEmpty(dbConnectionString))
        {
            healthChecksBuilder.AddNpgSql(
                dbConnectionString,
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "database" },
                timeout: TimeSpan.FromSeconds(5));
        }

        // Redis health check
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready", "cache" },
                timeout: TimeSpan.FromSeconds(5));
        }

        // RabbitMQ health check (custom implementation)
        var rabbitMqHost = configuration["RabbitMQ:Host"];
        if (!string.IsNullOrEmpty(rabbitMqHost))
        {
            healthChecksBuilder.AddCheck<RabbitMqHealthCheck>(
                "rabbitmq",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready", "messaging" });
        }

        // Outbox health check
        healthChecksBuilder.AddCheck<OutboxHealthCheck>(
            "outbox",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "messaging" });

        // Dead letter queue health check
        healthChecksBuilder.AddCheck<DeadLetterHealthCheck>(
            "deadletter",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "messaging" });

        return services;
    }

    /// <summary>
    /// Map health check endpoints for Kubernetes probes
    /// </summary>
    public static IApplicationBuilder UseHealthCheckEndpoints(this IApplicationBuilder app)
    {
        // Liveness probe - is the application alive?
        // Used by Kubernetes to determine if the container should be restarted
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        // Readiness probe - is the application ready to receive traffic?
        // Used by Kubernetes to determine if the pod should receive traffic
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        // Full health check - all checks
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        return app;
    }

    /// <summary>
    /// Custom JSON response writer for health checks
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Timestamp = DateTime.UtcNow,
            Checks = report.Entries.Select(e => new HealthCheckEntry
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
                Data = e.Value.Data.ToDictionary(d => d.Key, d => d.Value?.ToString()),
                Exception = e.Value.Exception?.Message,
                Tags = e.Value.Tags.ToList()
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

/// <summary>
/// Health check response model
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public List<HealthCheckEntry> Checks { get; set; } = new();
}

/// <summary>
/// Individual health check entry
/// </summary>
public class HealthCheckEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, string?>? Data { get; set; }
    public string? Exception { get; set; }
    public List<string>? Tags { get; set; }
}
