using EShop.Domain.Repositories;
using EShop.Infrastructure.Caching;
using EShop.Infrastructure.CQRS;
using EShop.Infrastructure.DistributedLocking;
using EShop.Infrastructure.EventSourcing;
using EShop.Infrastructure.Idempotency;
using EShop.Infrastructure.Messaging.Configuration;
using EShop.Infrastructure.Messaging.DeadLetter;
using EShop.Infrastructure.Messaging.Inbox;
using EShop.Infrastructure.Messaging.Publishers;
using EShop.Infrastructure.Observability;
using EShop.Infrastructure.Persistence;
using EShop.Infrastructure.Persistence.Outbox;
using EShop.Infrastructure.RateLimiting;
using EShop.Infrastructure.Repositories;
using EShop.Infrastructure.Resilience;
using EShop.Infrastructure.Scheduling;
using EShop.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EShop.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Add full production infrastructure with all features
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database (PostgreSQL)
        services.AddDatabase(configuration);

        // Redis (Cache, Locking, Rate Limiting)
        services.AddRedis(configuration);

        // Repositories
        services.AddRepositories();

        // Messaging (RabbitMQ, Inbox/Outbox, Sagas)
        services.AddMessagingInfrastructure(configuration);

        // Resilience (Polly)
        services.AddResiliencePipelines();

        // Observability (OpenTelemetry)
        services.AddObservability(configuration);

        // Security (Encryption)
        services.AddSecurity(configuration);

        // Scheduling (Retry Scheduler)
        services.AddScheduling();

        // Event Sourcing (Optional - enabled via config)
        if (configuration.GetValue<bool>("EventSourcing:Enabled"))
        {
            services.AddEventSourcing(configuration);
        }

        // CQRS Projections (Optional - enabled via config)
        if (configuration.GetValue<bool>("CQRS:Enabled"))
        {
            services.AddCQRS();
        }

        return services;
    }

    /// <summary>
    /// Add database services
    /// </summary>
    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Database connection string not found");

        services.AddDbContext<EShopDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(EShopDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
                npgsqlOptions.CommandTimeout(30);
            });
            
            // Enable sensitive data logging only in development
            if (configuration.GetValue<bool>("Development:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        return services;
    }

    /// <summary>
    /// Add Redis services (Cache, Distributed Locking, Rate Limiting)
    /// </summary>
    private static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        // Redis connection multiplexer (singleton)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        // Distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "eshop:";
        });

        // Cache service
        services.AddScoped<ICacheService, RedisCacheService>();

        // Distributed locking
        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

        // Rate limiting
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();

        // Idempotency
        services.AddScoped<IIdempotencyService, RedisIdempotencyService>();

        return services;
    }

    /// <summary>
    /// Add repository services
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();

        return services;
    }

    /// <summary>
    /// Add messaging infrastructure
    /// </summary>
    private static IServiceCollection AddMessagingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Inbox/Outbox services
        services.AddScoped<IInboxService, InboxService>();

        // Background processors
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<OutboxToRabbitMqPublisher>();
        services.AddHostedService<DeadLetterProcessor>();

        // RabbitMQ / MassTransit
        services.AddMessaging(configuration);

        return services;
    }

    /// <summary>
    /// Add security services
    /// </summary>
    private static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EncryptionSettings>(configuration.GetSection("Encryption"));
        services.AddSingleton<IMessageEncryption, AesMessageEncryption>();

        return services;
    }

    /// <summary>
    /// Add scheduling services
    /// </summary>
    private static IServiceCollection AddScheduling(this IServiceCollection services)
    {
        services.AddSingleton<IScheduledRetryRepository, InMemoryScheduledRetryRepository>();
        services.AddHostedService<RetrySchedulerService>();

        return services;
    }

    /// <summary>
    /// Add event sourcing services
    /// </summary>
    private static IServiceCollection AddEventSourcing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var eventStoreConnectionString = configuration.GetConnectionString("EventStore")
            ?? configuration.GetConnectionString("Database");

        services.AddDbContext<EventStoreDbContext>(options =>
        {
            options.UseNpgsql(eventStoreConnectionString);
        });

        services.AddScoped<IEventStore, PostgresEventStore>();

        return services;
    }

    /// <summary>
    /// Add CQRS projection services
    /// </summary>
    private static IServiceCollection AddCQRS(this IServiceCollection services)
    {
        // Projection processor background service
        services.AddHostedService<ProjectionProcessor>();

        return services;
    }

    /// <summary>
    /// Add infrastructure without messaging (simpler scenarios)
    /// </summary>
    public static IServiceCollection AddInfrastructureWithoutMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddRepositories();
        services.AddHostedService<OutboxProcessor>();
        services.AddResiliencePipelines();

        return services;
    }

    /// <summary>
    /// Add infrastructure for testing (in-memory everything)
    /// </summary>
    public static IServiceCollection AddInfrastructureForTesting(this IServiceCollection services)
    {
        // In-memory database
        services.AddDbContext<EShopDbContext>(options =>
        {
            options.UseInMemoryDatabase($"EShopTestDb_{Guid.NewGuid()}");
        });

        // Repositories
        services.AddRepositories();

        // In-memory implementations
        services.AddSingleton<IScheduledRetryRepository, InMemoryScheduledRetryRepository>();

        // In-memory messaging
        services.AddMessagingForTesting();

        return services;
    }

    /// <summary>
    /// Add minimal infrastructure for simple use cases
    /// </summary>
    public static IServiceCollection AddMinimalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddRepositories();

        return services;
    }
}
