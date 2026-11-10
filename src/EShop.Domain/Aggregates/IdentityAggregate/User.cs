using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.IdentityAggregate;

/// <summary>
/// User Aggregate Root - Core identity for authentication
/// 
/// BUSINESS RULES:
/// - Email must be unique across the system
/// - Password must meet complexity requirements (handled at application layer)
/// - Accounts can be locked after N failed login attempts
/// - Refresh tokens are single-use and have expiration
/// - Users can have multiple roles
/// </summary>
public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsLocked { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public int AccessFailedCount { get; private set; }
    public string? SecurityStamp { get; private set; }
    
    // Profile link (optional - for customers)
    public Guid? CustomerProfileId { get; private set; }
    public Guid? VendorId { get; private set; }
    
    private readonly List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    
    private readonly List<ExternalLogin> _externalLogins = new();
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();
    
    private readonly List<UserClaim> _claims = new();
    public IReadOnlyCollection<UserClaim> Claims => _claims.AsReadOnly();

    private User() { }

    public static User Create(string email, string passwordHash, params string[] roles)
    {
        ValidateEmail(email);
        
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsLocked = false,
            AccessFailedCount = 0,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        // Add roles
        foreach (var role in roles)
        {
            user._roles.Add(new UserRole(role));
        }

        // Default role if none specified
        if (!user._roles.Any())
        {
            user._roles.Add(new UserRole(AppRoles.Customer));
        }

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email));

        return user;
    }

    public static User CreateFromExternalLogin(
        string email, 
        string provider, 
        string providerKey,
        string? firstName = null,
        string? lastName = null)
    {
        ValidateEmail(email);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = string.Empty, // No password for external logins
            EmailConfirmed = true, // External providers verify email
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsLocked = false,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        user._roles.Add(new UserRole(AppRoles.Customer));
        user._externalLogins.Add(new ExternalLogin(provider, providerKey, firstName, lastName));
        
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email));

        return user;
    }

    #region Authentication

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AccessFailedCount = 0;
        IsLocked = false;
        LockoutEnd = null;
    }

    public void RecordFailedLogin(int maxFailedAttempts = 5, int lockoutMinutes = 15)
    {
        AccessFailedCount++;
        
        if (AccessFailedCount >= maxFailedAttempts)
        {
            IsLocked = true;
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            AddDomainEvent(new UserLockedOutEvent(Id, Email, LockoutEnd.Value));
        }
    }

    public bool IsLockedOut()
    {
        if (!IsLocked) return false;
        
        if (LockoutEnd.HasValue && LockoutEnd.Value <= DateTime.UtcNow)
        {
            // Lockout expired
            IsLocked = false;
            LockoutEnd = null;
            AccessFailedCount = 0;
            return false;
        }
        
        return true;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash is required");

        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens
        
        // Revoke all refresh tokens on password change
        foreach (var token in _refreshTokens.Where(t => !t.IsRevoked))
        {
            token.Revoke("Password changed");
        }
        
        AddDomainEvent(new PasswordChangedEvent(Id, Email));
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed)
            throw new DomainException("Email is already confirmed");

        EmailConfirmed = true;
        AddDomainEvent(new EmailConfirmedEvent(Id, Email));
    }

    #endregion

    #region Refresh Tokens

    public RefreshToken GenerateRefreshToken(string ipAddress, string userAgent, int expiryDays = 7)
    {
        // Revoke old tokens from same device
        var existingTokens = _refreshTokens
            .Where(t => !t.IsRevoked && t.UserAgent == userAgent)
            .ToList();

        foreach (var oldToken in existingTokens)
        {
            oldToken.Revoke("Replaced by new token");
        }

        var refreshToken = RefreshToken.Create(ipAddress, userAgent, expiryDays);
        _refreshTokens.Add(refreshToken);

        // Cleanup old revoked tokens (keep last 10)
        var tokensToRemove = _refreshTokens
            .Where(t => t.IsRevoked || t.IsExpired)
            .OrderBy(t => t.CreatedAt)
            .SkipLast(10)
            .ToList();

        foreach (var token in tokensToRemove)
        {
            _refreshTokens.Remove(token);
        }

        return refreshToken;
    }

    public RefreshToken? ValidateRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);

        if (refreshToken == null)
            return null;

        if (refreshToken.IsRevoked)
        {
            // Token reuse detected - revoke all tokens (potential attack)
            foreach (var t in _refreshTokens.Where(t => !t.IsRevoked))
            {
                t.Revoke("Potential token theft - all tokens revoked");
            }
            AddDomainEvent(new SuspiciousLoginActivityEvent(Id, Email, "Refresh token reuse detected"));
            return null;
        }

        if (refreshToken.IsExpired)
            return null;

        return refreshToken;
    }

    public void RevokeRefreshToken(string token, string reason)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);
        refreshToken?.Revoke(reason);
    }

    public void RevokeAllRefreshTokens(string reason)
    {
        foreach (var token in _refreshTokens.Where(t => !t.IsRevoked))
        {
            token.Revoke(reason);
        }
    }

    #endregion

    #region Roles

    public void AddRole(string role)
    {
        if (_roles.Any(r => r.Role.Equals(role, StringComparison.OrdinalIgnoreCase)))
            return;

        _roles.Add(new UserRole(role));
    }

    public void RemoveRole(string role)
    {
        var existingRole = _roles.FirstOrDefault(r => r.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
        if (existingRole != null)
        {
            _roles.Remove(existingRole);
        }
    }

    public bool HasRole(string role) => 
        _roles.Any(r => r.Role.Equals(role, StringComparison.OrdinalIgnoreCase));

    public bool IsAdmin => HasRole(AppRoles.Admin);
    public bool IsVendor => HasRole(AppRoles.Vendor);
    public bool IsCustomer => HasRole(AppRoles.Customer);

    #endregion

    #region External Logins

    public void AddExternalLogin(string provider, string providerKey, string? firstName = null, string? lastName = null)
    {
        if (_externalLogins.Any(l => l.Provider == provider))
            throw new DomainException($"External login for {provider} already exists");

        _externalLogins.Add(new ExternalLogin(provider, providerKey, firstName, lastName));
    }

    public void RemoveExternalLogin(string provider)
    {
        var login = _externalLogins.FirstOrDefault(l => l.Provider == provider);
        if (login != null)
        {
            // Must have password or another external login
            if (string.IsNullOrEmpty(PasswordHash) && _externalLogins.Count == 1)
                throw new DomainException("Cannot remove the only login method");

            _externalLogins.Remove(login);
        }
    }

    public ExternalLogin? GetExternalLogin(string provider) =>
        _externalLogins.FirstOrDefault(l => l.Provider == provider);

    #endregion

    #region Claims

    public void AddClaim(string type, string value)
    {
        if (_claims.Any(c => c.Type == type && c.Value == value))
            return;

        _claims.Add(new UserClaim(type, value));
    }

    public void RemoveClaim(string type, string value)
    {
        var claim = _claims.FirstOrDefault(c => c.Type == type && c.Value == value);
        if (claim != null)
        {
            _claims.Remove(claim);
        }
    }

    #endregion

    #region Profile Links

    public void LinkCustomerProfile(Guid customerProfileId)
    {
        CustomerProfileId = customerProfileId;
    }

    public void LinkVendor(Guid vendorId)
    {
        if (!HasRole(AppRoles.Vendor))
            AddRole(AppRoles.Vendor);
        
        VendorId = vendorId;
    }

    #endregion

    #region Account Management

    public void Deactivate()
    {
        IsActive = false;
        RevokeAllRefreshTokens("Account deactivated");
        AddDomainEvent(new UserDeactivatedEvent(Id, Email));
    }

    public void Reactivate()
    {
        IsActive = true;
        AddDomainEvent(new UserReactivatedEvent(Id, Email));
    }

    public void Unlock()
    {
        IsLocked = false;
        LockoutEnd = null;
        AccessFailedCount = 0;
    }

    public void UpdateSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid().ToString();
    }

    #endregion

    #region Two-Factor Authentication

    public void EnableTwoFactor(string secret)
    {
        TwoFactorSecret = secret;
        TwoFactorEnabled = true;
        SecurityStamp = Guid.NewGuid().ToString();
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        TwoFactorEnabled = false;
        SecurityStamp = Guid.NewGuid().ToString();
    }

    #endregion

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required");
        if (!email.Contains('@') || !email.Contains('.'))
            throw new DomainException("Invalid email format");
    }
}

/// <summary>
/// Application roles
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string Vendor = "Vendor";
    public const string Support = "Support";
    public const string Manager = "Manager";

    public static readonly string[] All = { Admin, Customer, Vendor, Support, Manager };
}
