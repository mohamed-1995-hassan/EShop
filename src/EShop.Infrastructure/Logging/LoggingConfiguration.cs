using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace EShop.Infrastructure.Logging;

/// <summary>
/// STRUCTURED LOGGING WITH SERILOG
/// 
/// Production logging requirements:
/// - Structured (JSON) for log aggregation
/// - Correlation IDs for distributed tracing
/// - Appropriate log levels
/// - Multiple sinks (Console, File, Seq/ELK)
/// </summary>
public static class LoggingConfiguration
{
    public static IServiceCollection AddStructuredLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "EShop.API");

        // Console (structured for Docker/K8s)
        logConfig.WriteTo.Console(new CompactJsonFormatter());

        // File (rolling, JSON)
        var logPath = configuration["Logging:FilePath"] ?? "logs/eshop-.log";
        logConfig.WriteTo.File(
            new CompactJsonFormatter(),
            logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 100_000_000); // 100MB

        // Seq (if configured)
        var seqUrl = configuration["Logging:SeqUrl"];
        if (!string.IsNullOrEmpty(seqUrl))
        {
            logConfig.WriteTo.Seq(seqUrl);
        }

        Log.Logger = logConfig.CreateLogger();

        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
        });

        return services;
    }
}

/// <summary>
/// Log scopes for contextual logging
/// </summary>
public static class LogScopes
{
    public static IDisposable? BeginOrderScope(Microsoft.Extensions.Logging.ILogger logger, Guid orderId, Guid customerId)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = orderId,
            ["CustomerId"] = customerId
        });
    }

    public static IDisposable? BeginPaymentScope(Microsoft.Extensions.Logging.ILogger logger, Guid paymentId, Guid orderId)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["PaymentId"] = paymentId,
            ["OrderId"] = orderId
        });
    }

    public static IDisposable? BeginMessageScope(Microsoft.Extensions.Logging.ILogger logger, Guid messageId, string messageType)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = messageId,
            ["MessageType"] = messageType
        });
    }
}
