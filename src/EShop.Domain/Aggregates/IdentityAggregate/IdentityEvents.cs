using EShop.Domain.Common;

namespace EShop.Domain.Aggregates.IdentityAggregate;

/// <summary>
/// Domain events for the Identity aggregate
/// </summary>

public class UserRegisteredEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public UserRegisteredEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class UserLoggedInEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string IpAddress { get; }
    
    public UserLoggedInEvent(Guid userId, string email, string ipAddress)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
    }
}

public class UserLockedOutEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public DateTime LockoutEnd { get; }
    
    public UserLockedOutEvent(Guid userId, string email, DateTime lockoutEnd)
    {
        UserId = userId;
        Email = email;
        LockoutEnd = lockoutEnd;
    }
}

public class PasswordChangedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public PasswordChangedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class EmailConfirmedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public EmailConfirmedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class PasswordResetRequestedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public PasswordResetRequestedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class UserDeactivatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public UserDeactivatedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class UserReactivatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public UserReactivatedEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class SuspiciousLoginActivityEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string Reason { get; }
    
    public SuspiciousLoginActivityEvent(Guid userId, string email, string reason)
    {
        UserId = userId;
        Email = email;
        Reason = reason;
    }
}

public class ExternalLoginAddedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Provider { get; }
    
    public ExternalLoginAddedEvent(Guid userId, string provider)
    {
        UserId = userId;
        Provider = provider;
    }
}

public class TwoFactorEnabledEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public TwoFactorEnabledEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class TwoFactorDisabledEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    
    public TwoFactorDisabledEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}

public class RoleAssignedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Role { get; }
    
    public RoleAssignedEvent(Guid userId, string role)
    {
        UserId = userId;
        Role = role;
    }
}

public class RoleRemovedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Role { get; }
    
    public RoleRemovedEvent(Guid userId, string role)
    {
        UserId = userId;
        Role = role;
    }
}
