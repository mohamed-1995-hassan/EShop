using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace EShop.API.Configuration;

/// <summary>
/// SERILOG CONFIGURATION FOR API
/// 
/// Provides structured logging for:
/// - Request/Response logging
/// - Performance tracking
/// - Error aggregation
/// - Distributed tracing support
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configure Serilog for the application
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "EShop.API")
                .Enrich.WithProperty("Version", GetApplicationVersion())
                .Enrich.WithCorrelationId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    "logs/eshop-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 100_000_000);

            // Add Seq if configured
            var seqUrl = context.Configuration["Logging:SeqUrl"];
            if (!string.IsNullOrEmpty(seqUrl))
            {
                configuration.WriteTo.Seq(seqUrl);
            }

            // Production-specific settings
            if (context.HostingEnvironment.IsProduction())
            {
                configuration.MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning);
                configuration.MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning);
                configuration.MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning);
            }
        });

        return builder;
    }

    /// <summary>
    /// Add Serilog request logging middleware
    /// </summary>
    public static IApplicationBuilder UseSerilogRequestLoggingMiddleware(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            // Customize the message template
            options.MessageTemplate = 
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            // Emit additional properties
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                
                // Add user info if authenticated
                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId", 
                        httpContext.User.FindFirst("sub")?.Value ?? 
                        httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                }

                // Add correlation ID
                if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId.FirstOrDefault());
                }
            };

            // Log levels based on response status
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                if (elapsed > 5000) // Slow requests
                    return LogEventLevel.Warning;

                // Skip health check endpoints at Info level
                var path = httpContext.Request.Path.Value?.ToLowerInvariant();
                if (path != null && (path.Contains("/health") || path.Contains("/ready") || path.Contains("/live")))
                    return LogEventLevel.Debug;

                return LogEventLevel.Information;
            };
        });

        return app;
    }

    private static string GetApplicationVersion()
    {
        return typeof(SerilogConfiguration).Assembly
            .GetName()
            .Version?
            .ToString() ?? "1.0.0";
    }
}

/// <summary>
/// Enricher to add correlation ID from request headers or generate new one
/// </summary>
public static class CorrelationIdEnricherExtensions
{
    public static LoggerConfiguration WithCorrelationId(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        return enrichmentConfiguration.With<CorrelationIdEnricher>();
    }
}

public class CorrelationIdEnricher : Serilog.Core.ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdEnricher() : this(new HttpContextAccessor())
    {
    }

    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(Serilog.Events.LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? _httpContextAccessor.HttpContext?.TraceIdentifier
            ?? Guid.NewGuid().ToString();

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
    }
}
