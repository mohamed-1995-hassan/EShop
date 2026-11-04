namespace EShop.Domain.Common;

/// <summary>
/// Base class for Aggregate Roots
/// An aggregate root is the entry point to an aggregate
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    // Aggregate roots can have additional behavior specific to being a root
    // For example, version tracking for optimistic concurrency
    public int Version { get; protected set; }
}
