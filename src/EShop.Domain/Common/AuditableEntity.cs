namespace EShop.Domain.Common;

/// <summary>
/// Base class for auditable entities.
/// 
/// Combines Entity<TId> with audit trail capabilities.
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity 
    where TId : notnull
{
    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }
    
    /// <summary>
    /// Set creation audit fields.
    /// Called by infrastructure when entity is first persisted.
    /// </summary>
    public void SetCreatedAudit(string? userId)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = userId;
    }
    
    /// <summary>
    /// Set modification audit fields.
    /// Called by infrastructure when entity is updated.
    /// </summary>
    public void SetModifiedAudit(string? userId)
    {
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = userId;
    }
}

/// <summary>
/// Base class for entities with full audit trail including soft delete.
/// </summary>
public abstract class FullyAuditableEntity<TId> : AuditableEntity<TId>, ISoftDeletable 
    where TId : notnull
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }
    
    /// <summary>
    /// Soft delete the entity.
    /// </summary>
    public virtual void Delete(string? userId)
    {
        if (IsDeleted) return;
        
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = userId;
    }
    
    /// <summary>
    /// Restore a soft-deleted entity.
    /// </summary>
    public virtual void Restore(string? userId)
    {
        if (!IsDeleted) return;
        
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        SetModifiedAudit(userId);
    }
}

/// <summary>
/// For aggregate roots that need full auditing
/// </summary>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity 
    where TId : notnull
{
    public DateTime CreatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTime? ModifiedAt { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    
    public void SetCreatedAudit(string? userId)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = userId;
    }
    
    public void SetModifiedAudit(string? userId)
    {
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = userId;
    }
}

/// <summary>
/// For aggregate roots that need soft delete
/// </summary>
public abstract class FullyAuditableAggregateRoot<TId> : AuditableAggregateRoot<TId>, ISoftDeletable 
    where TId : notnull
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }
    
    public virtual void Delete(string? userId)
    {
        if (IsDeleted) return;
        
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = userId;
    }
    
    public virtual void Restore(string? userId)
    {
        if (!IsDeleted) return;
        
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        SetModifiedAudit(userId);
    }
}
