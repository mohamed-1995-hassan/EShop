namespace EShop.Domain.Common;

/// <summary>
/// Base class for all entities with identity.
/// 
/// KEY CONCEPTS:
/// - Entities are defined by their IDENTITY, not their attributes
/// - Two entities with same ID are considered equal
/// - Domain events are collected and published after save
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    /// <summary>
    /// The unique identifier. Once set, never changes.
    /// </summary>
    public TId Id { get; protected set; } = default!;
    
    /// <summary>
    /// Domain events that occurred during this aggregate's operations.
    /// These are collected and published AFTER the save is successful.
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Add a domain event to be published after save.
    /// Called from within aggregate methods when something important happens.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    /// <summary>
    /// Clear all pending domain events.
    /// Called by infrastructure after events are published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    // Equality based on ID, not properties
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
        
        if (GetType() != other.GetType())
            return false;
        
        return Id.Equals(other.Id);
    }
    
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
