using EShop.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace EShop.Infrastructure.Persistence;

/// <summary>
/// EF Core Interceptor for automatic audit trail population.
/// 
/// Automatically sets:
/// - CreatedAt, CreatedBy on insert
/// - ModifiedAt, ModifiedBy on update
/// - DeletedAt, DeletedBy on soft delete
/// 
/// Uses IHttpContextAccessor to get current user from JWT claims.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null) return;

        var userId = GetCurrentUserId();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetCreatedFields(entry.Entity, userId, utcNow);
                    break;

                case EntityState.Modified:
                    SetModifiedFields(entry.Entity, userId, utcNow);
                    break;
            }
        }

        // Handle soft delete
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Modified)
            {
                var originalIsDeleted = entry.OriginalValues.GetValue<bool>(nameof(ISoftDeletable.IsDeleted));
                var currentIsDeleted = entry.CurrentValues.GetValue<bool>(nameof(ISoftDeletable.IsDeleted));

                // Entity is being soft deleted
                if (!originalIsDeleted && currentIsDeleted)
                {
                    SetDeletedFields(entry.Entity, userId, utcNow);
                }
            }
        }
    }

    private void SetCreatedFields(IAuditableEntity entity, string? userId, DateTime utcNow)
    {
        // Use reflection to set protected properties
        var entityType = entity.GetType();
        
        var createdAtProp = entityType.GetProperty(nameof(IAuditableEntity.CreatedAt));
        var createdByProp = entityType.GetProperty(nameof(IAuditableEntity.CreatedBy));
        
        if (createdAtProp?.CanWrite == true)
            createdAtProp.SetValue(entity, utcNow);
        else
            SetPrivateProperty(entity, "CreatedAt", utcNow);
            
        if (createdByProp?.CanWrite == true)
            createdByProp.SetValue(entity, userId);
        else
            SetPrivateProperty(entity, "CreatedBy", userId);
    }

    private void SetModifiedFields(IAuditableEntity entity, string? userId, DateTime utcNow)
    {
        var entityType = entity.GetType();
        
        var modifiedAtProp = entityType.GetProperty(nameof(IAuditableEntity.ModifiedAt));
        var modifiedByProp = entityType.GetProperty(nameof(IAuditableEntity.ModifiedBy));
        
        if (modifiedAtProp?.CanWrite == true)
            modifiedAtProp.SetValue(entity, utcNow);
        else
            SetPrivateProperty(entity, "ModifiedAt", utcNow);
            
        if (modifiedByProp?.CanWrite == true)
            modifiedByProp.SetValue(entity, userId);
        else
            SetPrivateProperty(entity, "ModifiedBy", userId);
    }

    private void SetDeletedFields(ISoftDeletable entity, string? userId, DateTime utcNow)
    {
        var entityType = entity.GetType();
        
        var deletedAtProp = entityType.GetProperty(nameof(ISoftDeletable.DeletedAt));
        var deletedByProp = entityType.GetProperty(nameof(ISoftDeletable.DeletedBy));
        
        if (deletedAtProp?.CanWrite == true)
            deletedAtProp.SetValue(entity, utcNow);
        else
            SetPrivateProperty(entity, "DeletedAt", utcNow);
            
        if (deletedByProp?.CanWrite == true)
            deletedByProp.SetValue(entity, userId);
        else
            SetPrivateProperty(entity, "DeletedBy", userId);
    }

    private static void SetPrivateProperty(object entity, string propertyName, object? value)
    {
        var property = entity.GetType().GetProperty(propertyName, 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic);
            
        if (property != null)
        {
            var backingField = entity.GetType().GetField($"<{propertyName}>k__BackingField", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
                
            if (backingField != null)
            {
                backingField.SetValue(entity, value);
            }
        }
    }

    private string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        // Try common claim types for user ID
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("userId")?.Value;
    }
}

/// <summary>
/// Extension to register audit interceptor
/// </summary>
public static class AuditInterceptorExtensions
{
    public static IServiceCollection AddAuditInterceptor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuditInterceptor>();
        return services;
    }
}
