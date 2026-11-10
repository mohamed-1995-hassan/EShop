using EShop.Domain.Aggregates.IdentityAggregate;
using EShop.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Identity;

/// <summary>
/// Core authentication service
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, string ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ConfirmEmailResult> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationRepository;
    private readonly IPasswordResetTokenRepository _passwordResetRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly PasswordSettings _passwordSettings;

    public AuthenticationService(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository emailVerificationRepository,
        IPasswordResetTokenRepository passwordResetRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        IOptions<PasswordSettings> passwordSettings)
    {
        _userRepository = userRepository;
        _emailVerificationRepository = emailVerificationRepository;
        _passwordResetRepository = passwordResetRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _passwordSettings = passwordSettings.Value;
    }

    public async Task<AuthenticationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        if (await _userRepository.ExistsAsync(request.Email, cancellationToken))
        {
            return AuthenticationResult.Failed("A user with this email already exists");
        }

        // Validate password
        var passwordValidation = _passwordHasher.ValidatePassword(request.Password);
        if (!passwordValidation.IsValid)
        {
            return AuthenticationResult.Failed(string.Join(", ", passwordValidation.Errors));
        }

        // Create user
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, AppRoles.Customer);

        await _userRepository.AddAsync(user, cancellationToken);

        // Create email verification token
        var verificationToken = EmailVerificationToken.Create(user.Id);
        await _emailVerificationRepository.AddAsync(verificationToken, cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = user.GenerateRefreshToken(request.IpAddress, request.UserAgent, _jwtSettings.RefreshTokenExpirationDays);
        
        await _userRepository.UpdateAsync(user, cancellationToken);

        return AuthenticationResult.Success(
            accessToken,
            refreshToken.Token,
            user.Id,
            user.Email,
            user.Roles.Select(r => r.Role).ToList(),
            requiresEmailConfirmation: true,
            emailVerificationToken: verificationToken.Token
        );
    }

    public async Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            return AuthenticationResult.Failed("Invalid email or password");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            return AuthenticationResult.Failed("Account is deactivated");
        }

        // Check if locked out
        if (user.IsLockedOut())
        {
            var remainingMinutes = (user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes;
            return AuthenticationResult.Failed($"Account is locked. Try again in {Math.Ceiling(remainingMinutes)} minutes");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(_passwordSettings.MaxFailedAttempts, _passwordSettings.LockoutMinutes);
            await _userRepository.UpdateAsync(user, cancellationToken);
            return AuthenticationResult.Failed("Invalid email or password");
        }

        // Successful login
        user.RecordSuccessfulLogin();

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = user.GenerateRefreshToken(request.IpAddress, request.UserAgent, _jwtSettings.RefreshTokenExpirationDays);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return AuthenticationResult.Success(
            accessToken,
            refreshToken.Token,
            user.Id,
            user.Email,
            user.Roles.Select(r => r.Role).ToList(),
            requiresEmailConfirmation: !user.EmailConfirmed
        );
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, string ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);

        if (user == null)
        {
            return AuthenticationResult.Failed("Invalid refresh token");
        }

        var existingToken = user.ValidateRefreshToken(refreshToken);
        if (existingToken == null)
        {
            return AuthenticationResult.Failed("Invalid or expired refresh token");
        }

        // Check if account is still active
        if (!user.IsActive)
        {
            return AuthenticationResult.Failed("Account is deactivated");
        }

        // Rotate refresh token
        existingToken.Revoke("Replaced by new token", ipAddress);
        var newRefreshToken = user.GenerateRefreshToken(ipAddress, userAgent, _jwtSettings.RefreshTokenExpirationDays);

        // Generate new access token
        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return AuthenticationResult.Success(
            accessToken,
            newRefreshToken.Token,
            user.Id,
            user.Email,
            user.Roles.Select(r => r.Role).ToList()
        );
    }

    public async Task<AuthenticationResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default)
    {
        // Check if external login exists
        var user = await _userRepository.GetByExternalLoginAsync(request.Provider, request.ProviderKey, cancellationToken);

        if (user == null)
        {
            // Check if email is already registered
            user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user != null)
            {
                // Link external login to existing account
                user.AddExternalLogin(request.Provider, request.ProviderKey, request.FirstName, request.LastName);
            }
            else
            {
                // Create new user from external login
                user = User.CreateFromExternalLogin(
                    request.Email,
                    request.Provider,
                    request.ProviderKey,
                    request.FirstName,
                    request.LastName
                );
                await _userRepository.AddAsync(user, cancellationToken);
            }
        }

        // Check if account is active
        if (!user.IsActive)
        {
            return AuthenticationResult.Failed("Account is deactivated");
        }

        // Update last login
        user.RecordSuccessfulLogin();

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = user.GenerateRefreshToken(request.IpAddress, request.UserAgent, _jwtSettings.RefreshTokenExpirationDays);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return AuthenticationResult.Success(
            accessToken,
            refreshToken.Token,
            user.Id,
            user.Email,
            user.Roles.Select(r => r.Role).ToList(),
            isNewUser: user.CreatedAt > DateTime.UtcNow.AddMinutes(-1)
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);

        if (user == null) return;

        user.RevokeRefreshToken(refreshToken, $"Revoked from IP: {ipAddress}");
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var principal = _jwtTokenService.ValidateToken(accessToken);
        if (principal == null) return false;

        var userId = _jwtTokenService.GetUserIdFromToken(accessToken);
        if (!userId.HasValue) return false;

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null || !user.IsActive) return false;

        // Validate security stamp
        var securityStampClaim = principal.Claims.FirstOrDefault(c => c.Type == "security_stamp");
        if (securityStampClaim?.Value != user.SecurityStamp) return false;

        return true;
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Always return success to prevent email enumeration
        if (user == null || !user.IsActive)
        {
            return new ForgotPasswordResult(true, null);
        }

        var token = PasswordResetToken.Create(user.Id);
        await _passwordResetRepository.AddAsync(token, cancellationToken);

        return new ForgotPasswordResult(true, token.Token);
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var token = await _passwordResetRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token == null || !token.IsValid)
        {
            return new ResetPasswordResult(false, "Invalid or expired token");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            return new ResetPasswordResult(false, "User not found");
        }

        // Validate new password
        var passwordValidation = _passwordHasher.ValidatePassword(request.NewPassword);
        if (!passwordValidation.IsValid)
        {
            return new ResetPasswordResult(false, string.Join(", ", passwordValidation.Errors));
        }

        // Update password
        var passwordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(passwordHash);
        
        token.MarkAsUsed();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _passwordResetRepository.UpdateAsync(token, cancellationToken);

        return new ResetPasswordResult(true, null);
    }

    public async Task<ConfirmEmailResult> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var verificationToken = await _emailVerificationRepository.GetByTokenAsync(token, cancellationToken);

        if (verificationToken == null || !verificationToken.IsValid)
        {
            return new ConfirmEmailResult(false, "Invalid or expired token");
        }

        var user = await _userRepository.GetByIdAsync(verificationToken.UserId, cancellationToken);
        if (user == null)
        {
            return new ConfirmEmailResult(false, "User not found");
        }

        if (user.EmailConfirmed)
        {
            return new ConfirmEmailResult(true, null);
        }

        user.ConfirmEmail();
        verificationToken.MarkAsUsed();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _emailVerificationRepository.UpdateAsync(verificationToken, cancellationToken);

        return new ConfirmEmailResult(true, null);
    }
}

#region Request/Response Models

public record RegisterRequest(
    string Email,
    string Password,
    string IpAddress,
    string? UserAgent = null
);

public record LoginRequest(
    string Email,
    string Password,
    string IpAddress,
    string? UserAgent = null
);

public record ExternalLoginRequest(
    string Provider,
    string ProviderKey,
    string Email,
    string IpAddress,
    string? UserAgent = null,
    string? FirstName = null,
    string? LastName = null
);

public record ResetPasswordRequest(
    string Token,
    string NewPassword
);

public class AuthenticationResult
{
    public bool Succeeded { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = Array.Empty<string>();
    public string? ErrorMessage { get; private set; }
    public bool RequiresEmailConfirmation { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public bool IsNewUser { get; private set; }

    private AuthenticationResult() { }

    public static AuthenticationResult Success(
        string accessToken,
        string refreshToken,
        Guid userId,
        string email,
        IReadOnlyList<string> roles,
        bool requiresEmailConfirmation = false,
        string? emailVerificationToken = null,
        bool isNewUser = false)
    {
        return new AuthenticationResult
        {
            Succeeded = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = userId,
            Email = email,
            Roles = roles,
            RequiresEmailConfirmation = requiresEmailConfirmation,
            EmailVerificationToken = emailVerificationToken,
            IsNewUser = isNewUser
        };
    }

    public static AuthenticationResult Failed(string errorMessage)
    {
        return new AuthenticationResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }
}

public record ForgotPasswordResult(bool Succeeded, string? Token);
public record ResetPasswordResult(bool Succeeded, string? ErrorMessage);
public record ConfirmEmailResult(bool Succeeded, string? ErrorMessage);

#endregion
