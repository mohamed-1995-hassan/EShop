using EShop.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.DomainEvents;

/// <summary>
/// Dispatches domain events to their handlers using MediatR.
/// 
/// KEY POINTS:
/// - Called after successful save to database
/// - Events are dispatched in order they were raised
/// - Each event can have multiple handlers
/// - Handlers execute independently
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}

public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRDomainEventDispatcher> _logger;
    
    public MediatRDomainEventDispatcher(
        IMediator mediator,
        ILogger<MediatRDomainEventDispatcher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task DispatchEventsAsync(
        IEnumerable<IDomainEvent> events, 
        CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        
        if (!eventsList.Any())
            return;
        
        _logger.LogInformation(
            "🎯 Dispatching {Count} domain event(s)...",
            eventsList.Count);
        
        foreach (var domainEvent in eventsList)
        {
            _logger.LogInformation(
                "📢 Publishing event: {EventType} (ID: {EventId})",
                domainEvent.GetType().Name,
                domainEvent.EventId);
            
            try
            {
                // MediatR finds all handlers for this event type
                await _mediator.Publish(domainEvent, cancellationToken);
                
                _logger.LogInformation(
                    "✅ Event {EventType} processed successfully",
                    domainEvent.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error processing event {EventType}",
                    domainEvent.GetType().Name);
                
                // In production: might want to publish to error queue
                throw;
            }
        }
    }
}
