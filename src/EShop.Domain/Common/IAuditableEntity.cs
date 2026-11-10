namespace EShop.Domain.Common;

/// <summary>
/// AUDIT TRAIL PATTERN
/// 
/// Tracks who created/modified entities and when.
/// Essential for:
/// - Compliance (GDPR, SOX, HIPAA)
/// - Debugging and troubleshooting
/// - Security auditing
/// - Business intelligence
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// When the entity was created
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// User ID who created the entity
    /// </summary>
    string? CreatedBy { get; }
    
    /// <summary>
    /// When the entity was last modified
    /// </summary>
    DateTime? ModifiedAt { get; }
    
    /// <summary>
    /// User ID who last modified the entity
    /// </summary>
    string? ModifiedBy { get; }
}

/// <summary>
/// SOFT DELETE PATTERN
/// 
/// Instead of permanently deleting records:
/// - Marks them as deleted
/// - Preserves data for audit/recovery
/// - Maintains referential integrity
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Whether the entity is soft-deleted
    /// </summary>
    bool IsDeleted { get; }
    
    /// <summary>
    /// When the entity was deleted
    /// </summary>
    DateTime? DeletedAt { get; }
    
    /// <summary>
    /// User ID who deleted the entity
    /// </summary>
    string? DeletedBy { get; }
}

/// <summary>
/// Combined interface for entities that need both patterns
/// </summary>
public interface IFullyAuditableEntity : IAuditableEntity, ISoftDeletable
{
}
