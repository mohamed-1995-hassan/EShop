using MediatR;

namespace EShop.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Implements INotification for MediatR integration.
/// 
/// Domain events represent something that happened in the domain
/// that other parts of the system might be interested in.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredOn { get; }
    
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    Guid EventId { get; }
}

/// <summary>
/// Base class for domain events with common functionality.
/// All domain events should inherit from this.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; }
    public Guid EventId { get; }
    
    protected DomainEvent()
    {
        OccurredOn = DateTime.UtcNow;
        EventId = Guid.NewGuid();
    }
}
