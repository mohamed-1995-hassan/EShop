using EShop.Domain.Common;
using System.Security.Cryptography;

namespace EShop.Domain.Aggregates.IdentityAggregate;

/// <summary>
/// User Role Entity
/// </summary>
public class UserRole : Entity<Guid>
{
    public string Role { get; private set; }
    public DateTime AssignedAt { get; private set; }

    internal UserRole(string role)
    {
        Id = Guid.NewGuid();
        Role = role;
        AssignedAt = DateTime.UtcNow;
    }

    private UserRole() { } // EF Core
}

/// <summary>
/// Refresh Token Entity - Used for token rotation
/// </summary>
public class RefreshToken : Entity<Guid>
{
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { } // EF Core

    internal static RefreshToken Create(string ipAddress, string? userAgent, int expiryDays = 7)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            IsRevoked = false
        };
    }

    public void Revoke(string reason, string? ipAddress = null, string? replacementToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        RevokedReason = reason;
        ReplacedByToken = replacementToken;
    }

    private static string GenerateToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

/// <summary>
/// External Login (OAuth providers)
/// </summary>
public class ExternalLogin : Entity<Guid>
{
    public string Provider { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public DateTime LinkedAt { get; private set; }

    internal ExternalLogin(string provider, string providerKey, string? firstName = null, string? lastName = null)
    {
        Id = Guid.NewGuid();
        Provider = provider;
        ProviderKey = providerKey;
        FirstName = firstName;
        LastName = lastName;
        DisplayName = $"{firstName} {lastName}".Trim();
        LinkedAt = DateTime.UtcNow;
    }

    private ExternalLogin() { } // EF Core

    internal void UpdateProfile(string? firstName, string? lastName, string? profilePictureUrl)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = $"{firstName} {lastName}".Trim();
        ProfilePictureUrl = profilePictureUrl;
    }
}

/// <summary>
/// User Claim Entity
/// </summary>
public class UserClaim : Entity<Guid>
{
    public string Type { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    internal UserClaim(string type, string value)
    {
        Id = Guid.NewGuid();
        Type = type;
        Value = value;
        CreatedAt = DateTime.UtcNow;
    }

    private UserClaim() { } // EF Core
}

/// <summary>
/// Email verification token
/// </summary>
public class EmailVerificationToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    private EmailVerificationToken() { } // EF Core

    public static EmailVerificationToken Create(Guid userId, int expiryHours = 24)
    {
        return new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    private static string GenerateToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

/// <summary>
/// Password reset token
/// </summary>
public class PasswordResetToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    private PasswordResetToken() { } // EF Core

    public static PasswordResetToken Create(Guid userId, int expiryHours = 1)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    private static string GenerateToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

/// <summary>
/// OAuth Providers
/// </summary>
public static class OAuthProviders
{
    public const string Google = "Google";
    public const string Facebook = "Facebook";
    public const string Apple = "Apple";
    public const string Microsoft = "Microsoft";
    public const string Twitter = "Twitter";
    public const string GitHub = "GitHub";
}
