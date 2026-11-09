using EShop.Infrastructure.Messaging.Consumers;
using EShop.Infrastructure.Messaging.Sagas;
using EShop.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Infrastructure.Messaging.Configuration;

/// <summary>
/// RabbitMQ Configuration with:
/// - Dead Letter Queue (DLQ) for failed messages
/// - Retry policies with exponential backoff
/// - Circuit breaker for resilience
/// - Saga state machine with EF Core persistence
/// </summary>
public static class RabbitMqConfiguration
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitMqSettings = configuration.GetSection("RabbitMQ").Get<RabbitMqSettings>()
            ?? new RabbitMqSettings();

        services.AddMassTransit(x =>
        {
            // Register Consumers
            x.AddConsumer<OrderPlacedConsumer, OrderPlacedConsumerDefinition>();
            x.AddConsumer<ReserveStockCommandConsumer, ReserveStockCommandConsumerDefinition>();
            x.AddConsumer<ReleaseStockCommandConsumer>();
            x.AddConsumer<ProcessPaymentCommandConsumer, ProcessPaymentCommandConsumerDefinition>();
            x.AddConsumer<ConfirmOrderCommandConsumer, ConfirmOrderCommandConsumerDefinition>();
            x.AddConsumer<CancelOrderCommandConsumer, CancelOrderCommandConsumerDefinition>();

            // Register Saga State Machine with EF Core persistence
            x.AddSagaStateMachine<OrderProcessingSaga, OrderProcessingSagaState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<EShopDbContext>();
                    
                    // Use Npgsql for PostgreSQL
                    r.UsePostgres();
                });

            // Configure RabbitMQ
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.Port, rabbitMqSettings.VirtualHost, h =>
                {
                    h.Username(rabbitMqSettings.Username);
                    h.Password(rabbitMqSettings.Password);
                    
                    // Heartbeat for connection monitoring
                    h.Heartbeat(TimeSpan.FromSeconds(60));
                    
                    // Request timeout
                    h.RequestedConnectionTimeout(TimeSpan.FromSeconds(30));
                });

                // Global retry policy
                cfg.UseMessageRetry(r => r
                    .Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5))
                    .Ignore<ValidationException>());

                // Global circuit breaker
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });

                // Dead Letter Queue configuration
                // Messages that fail after all retries go to DLQ for manual inspection
                cfg.ConfigureEndpoints(context, new DeadLetterQueueEndpointNameFormatter());

                // Saga endpoint
                cfg.ReceiveEndpoint("order-processing-saga", e =>
                {
                    e.ConfigureSaga<OrderProcessingSagaState>(context);
                    
                    // DLQ for saga - important for debugging stuck sagas
                    e.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromMinutes(5),
                        TimeSpan.FromMinutes(15),
                        TimeSpan.FromMinutes(30)));
                        
                    e.UseMessageRetry(r => r.Immediate(3));
                });

                // Configure all registered consumers/sagas
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// For testing - use in-memory transport
    /// </summary>
    public static IServiceCollection AddMessagingForTesting(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderPlacedConsumer>();
            x.AddConsumer<ReserveStockCommandConsumer>();
            x.AddConsumer<ProcessPaymentCommandConsumer>();
            x.AddConsumer<ConfirmOrderCommandConsumer>();
            x.AddConsumer<CancelOrderCommandConsumer>();

            x.AddSagaStateMachine<OrderProcessingSaga, OrderProcessingSagaState>()
                .InMemoryRepository();

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

/// <summary>
/// Custom endpoint formatter that configures DLQ for all endpoints
/// </summary>
public class DeadLetterQueueEndpointNameFormatter : DefaultEndpointNameFormatter
{
    public DeadLetterQueueEndpointNameFormatter() : base(includeNamespace: false)
    {
    }
}

/// <summary>
/// Custom exception for validation failures that should not be retried
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
