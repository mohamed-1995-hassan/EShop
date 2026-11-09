using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Domain.Aggregates.ProductAggregate;
using EShop.Domain.Common;
using EShop.Domain.Entities;
using EShop.Infrastructure.Messaging.DeadLetter;
using EShop.Infrastructure.Messaging.Inbox;
using EShop.Infrastructure.Messaging.Sagas;
using EShop.Infrastructure.Persistence.Configurations;
using EShop.Infrastructure.Persistence.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext with DDD patterns:
/// - Complex Type mapping for Value Objects
/// - Domain Event dispatching after SaveChanges
/// - Outbox pattern for reliable event publishing
/// - Inbox pattern for idempotent message processing
/// - Saga state persistence
/// - Dead Letter Queue storage
/// </summary>
public class EShopDbContext : DbContext
{
    private readonly IMediator? _mediator;

    public EShopDbContext(DbContextOptions<EShopDbContext> options) : base(options) { }

    public EShopDbContext(DbContextOptions<EShopDbContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    // Aggregates
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();

    // Outbox for reliable event publishing
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    
    // Inbox for idempotent message processing
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    
    // Saga state persistence
    public DbSet<OrderProcessingSagaState> OrderProcessingSagas => Set<OrderProcessingSagaState>();
    
    // Dead Letter Queue
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EShopDbContext).Assembly);

        // Alternative: Apply individual configurations
        // modelBuilder.ApplyConfiguration(new OrderConfiguration());
        // modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        // modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }

    /// <summary>
    /// Override SaveChanges to dispatch domain events after successful save
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving
        var domainEvents = CollectDomainEvents();

        // Save changes to database
        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        if (_mediator != null)
        {
            await DispatchDomainEventsAsync(domainEvents, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Save changes with outbox pattern - events stored in same transaction
    /// </summary>
    public async Task<int> SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
    {
        // Collect and convert domain events to outbox messages
        var domainEvents = CollectDomainEvents();
        
        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = OutboxMessage.FromDomainEvent(domainEvent);
            OutboxMessages.Add(outboxMessage);
        }

        // Save both entity changes and outbox messages in same transaction
        return await base.SaveChangesAsync(cancellationToken);
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var entities = ChangeTracker
            .Entries<Entity<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events after collecting
        entities.ForEach(e => e.ClearDomainEvents());

        return domainEvents;
    }

    private async Task DispatchDomainEventsAsync(List<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _mediator!.Publish(domainEvent, cancellationToken);
        }
    }
}
