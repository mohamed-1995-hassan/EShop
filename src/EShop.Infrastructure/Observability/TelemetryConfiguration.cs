using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EShop.Infrastructure.Observability;

/// <summary>
/// OPENTELEMETRY CONFIGURATION
/// 
/// Provides:
/// - Distributed Tracing (traces requests across services)
/// - Metrics (counters, histograms, gauges)
/// - Logging correlation
/// 
/// EXPORTERS:
/// - OTLP (Jaeger, Zipkin, Tempo)
/// - Prometheus (metrics)
/// </summary>
public static class TelemetryConfiguration
{
    public const string ServiceName = "EShop.API";
    public const string ServiceVersion = "1.0.0";

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: ServiceName,
                    serviceVersion: ServiceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = configuration["Environment"] ?? "development",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                // Instrumentation
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = context =>
                    {
                        // Filter out health checks
                        var path = context.Request.Path.Value ?? "";
                        return !path.Contains("health") && !path.Contains("metrics");
                    };
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress);
                        activity.SetTag("user.id", request.HttpContext.User?.Identity?.Name);
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.FilterHttpRequestMessage = request =>
                    {
                        // Filter internal health checks
                        return request.RequestUri?.PathAndQuery.Contains("health") != true;
                    };
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                })
                // Custom sources
                .AddSource(ActivitySources.Orders)
                .AddSource(ActivitySources.Payments)
                .AddSource(ActivitySources.Inventory)
                .AddSource(ActivitySources.Messaging)
                // Exporters
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }))
            .WithMetrics(metrics => metrics
                // Instrumentation
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // Custom meters
                .AddMeter(MetricNames.Orders)
                .AddMeter(MetricNames.Payments)
                .AddMeter(MetricNames.Inventory)
                .AddMeter(MetricNames.Messaging)
                // Exporters
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                })
                .AddPrometheusExporter());

        // Register custom metrics
        services.AddSingleton<ApplicationMetrics>();
        services.AddSingleton<MessagingMetrics>();

        return services;
    }
}

/// <summary>
/// Activity sources for distributed tracing
/// </summary>
public static class ActivitySources
{
    public const string Orders = "EShop.Orders";
    public const string Payments = "EShop.Payments";
    public const string Inventory = "EShop.Inventory";
    public const string Messaging = "EShop.Messaging";

    public static readonly ActivitySource OrdersSource = new(Orders);
    public static readonly ActivitySource PaymentsSource = new(Payments);
    public static readonly ActivitySource InventorySource = new(Inventory);
    public static readonly ActivitySource MessagingSource = new(Messaging);
}

/// <summary>
/// Metric names for Prometheus
/// </summary>
public static class MetricNames
{
    public const string Orders = "eshop_orders";
    public const string Payments = "eshop_payments";
    public const string Inventory = "eshop_inventory";
    public const string Messaging = "eshop_messaging";
}

/// <summary>
/// Application-level metrics
/// </summary>
public class ApplicationMetrics
{
    private readonly Counter<long> _ordersCreated;
    private readonly Counter<long> _ordersCompleted;
    private readonly Counter<long> _ordersCancelled;
    private readonly Histogram<double> _orderProcessingDuration;
    private readonly Counter<long> _paymentsProcessed;
    private readonly Counter<long> _paymentsFailed;
    private readonly Histogram<double> _paymentProcessingDuration;

    public ApplicationMetrics()
    {
        var meter = new Meter(MetricNames.Orders, "1.0.0");

        _ordersCreated = meter.CreateCounter<long>(
            "orders_created_total",
            description: "Total number of orders created");

        _ordersCompleted = meter.CreateCounter<long>(
            "orders_completed_total",
            description: "Total number of orders completed");

        _ordersCancelled = meter.CreateCounter<long>(
            "orders_cancelled_total",
            description: "Total number of orders cancelled");

        _orderProcessingDuration = meter.CreateHistogram<double>(
            "order_processing_duration_seconds",
            unit: "s",
            description: "Order processing duration in seconds");

        var paymentMeter = new Meter(MetricNames.Payments, "1.0.0");

        _paymentsProcessed = paymentMeter.CreateCounter<long>(
            "payments_processed_total",
            description: "Total number of payments processed");

        _paymentsFailed = paymentMeter.CreateCounter<long>(
            "payments_failed_total",
            description: "Total number of failed payments");

        _paymentProcessingDuration = paymentMeter.CreateHistogram<double>(
            "payment_processing_duration_seconds",
            unit: "s",
            description: "Payment processing duration in seconds");
    }

    public void RecordOrderCreated(string status = "pending") =>
        _ordersCreated.Add(1, new KeyValuePair<string, object?>("status", status));

    public void RecordOrderCompleted() => _ordersCompleted.Add(1);

    public void RecordOrderCancelled(string reason) =>
        _ordersCancelled.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordOrderProcessingDuration(double seconds, string status) =>
        _orderProcessingDuration.Record(seconds, new KeyValuePair<string, object?>("status", status));

    public void RecordPaymentProcessed(string method) =>
        _paymentsProcessed.Add(1, new KeyValuePair<string, object?>("method", method));

    public void RecordPaymentFailed(string reason) =>
        _paymentsFailed.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordPaymentDuration(double seconds, bool success) =>
        _paymentProcessingDuration.Record(seconds, new KeyValuePair<string, object?>("success", success));
}

/// <summary>
/// Messaging-specific metrics
/// </summary>
public class MessagingMetrics
{
    private readonly Counter<long> _messagesPublished;
    private readonly Counter<long> _messagesConsumed;
    private readonly Counter<long> _messagesFailed;
    private readonly Counter<long> _messagesRetried;
    private readonly Histogram<double> _messageProcessingDuration;
    private readonly Counter<long> _deadLetterMessages;
    private readonly Counter<long> _sagasStarted;
    private readonly Counter<long> _sagasCompleted;
    private readonly Counter<long> _sagasFailed;

    public MessagingMetrics()
    {
        var meter = new Meter(MetricNames.Messaging, "1.0.0");

        _messagesPublished = meter.CreateCounter<long>(
            "messages_published_total",
            description: "Total messages published");

        _messagesConsumed = meter.CreateCounter<long>(
            "messages_consumed_total",
            description: "Total messages consumed");

        _messagesFailed = meter.CreateCounter<long>(
            "messages_failed_total",
            description: "Total messages that failed processing");

        _messagesRetried = meter.CreateCounter<long>(
            "messages_retried_total",
            description: "Total message retry attempts");

        _messageProcessingDuration = meter.CreateHistogram<double>(
            "message_processing_duration_seconds",
            unit: "s",
            description: "Message processing duration");

        _deadLetterMessages = meter.CreateCounter<long>(
            "dead_letter_messages_total",
            description: "Total messages sent to DLQ");

        _sagasStarted = meter.CreateCounter<long>(
            "sagas_started_total",
            description: "Total sagas started");

        _sagasCompleted = meter.CreateCounter<long>(
            "sagas_completed_total",
            description: "Total sagas completed successfully");

        _sagasFailed = meter.CreateCounter<long>(
            "sagas_failed_total",
            description: "Total sagas that failed");
    }

    public void RecordMessagePublished(string messageType) =>
        _messagesPublished.Add(1, new KeyValuePair<string, object?>("type", messageType));

    public void RecordMessageConsumed(string messageType, string consumer) =>
        _messagesConsumed.Add(1,
            new KeyValuePair<string, object?>("type", messageType),
            new KeyValuePair<string, object?>("consumer", consumer));

    public void RecordMessageFailed(string messageType, string error) =>
        _messagesFailed.Add(1,
            new KeyValuePair<string, object?>("type", messageType),
            new KeyValuePair<string, object?>("error", error));

    public void RecordMessageRetried(string messageType, int attempt) =>
        _messagesRetried.Add(1,
            new KeyValuePair<string, object?>("type", messageType),
            new KeyValuePair<string, object?>("attempt", attempt));

    public void RecordMessageProcessingDuration(double seconds, string messageType) =>
        _messageProcessingDuration.Record(seconds, new KeyValuePair<string, object?>("type", messageType));

    public void RecordDeadLetter(string messageType, string reason) =>
        _deadLetterMessages.Add(1,
            new KeyValuePair<string, object?>("type", messageType),
            new KeyValuePair<string, object?>("reason", reason));

    public void RecordSagaStarted(string sagaType) =>
        _sagasStarted.Add(1, new KeyValuePair<string, object?>("type", sagaType));

    public void RecordSagaCompleted(string sagaType) =>
        _sagasCompleted.Add(1, new KeyValuePair<string, object?>("type", sagaType));

    public void RecordSagaFailed(string sagaType, string reason) =>
        _sagasFailed.Add(1,
            new KeyValuePair<string, object?>("type", sagaType),
            new KeyValuePair<string, object?>("reason", reason));
}
