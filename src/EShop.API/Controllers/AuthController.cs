using EShop.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

/// <summary>
/// Authentication endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <remarks>
    /// Creates a new user with the Customer role.
    /// Returns JWT access token and refresh token.
    /// An email verification token is also returned (send it via email in production).
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();

        var result = await _authService.RegisterAsync(
            new RegisterRequest(request.Email, request.Password, ipAddress, userAgent),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Registration failed",
                Detail = result.ErrorMessage 
            });
        }

        SetRefreshTokenCookie(result.RefreshToken!);

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = 900, // 15 minutes
            UserId = result.UserId!.Value,
            Email = result.Email!,
            Roles = result.Roles.ToList(),
            RequiresEmailConfirmation = result.RequiresEmailConfirmation,
            EmailVerificationToken = result.EmailVerificationToken
        });
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <remarks>
    /// Returns JWT access token and refresh token on successful authentication.
    /// Account will be locked after 5 failed attempts for 15 minutes.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();

        var result = await _authService.LoginAsync(
            new LoginRequest(request.Email, request.Password, ipAddress, userAgent),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails 
            { 
                Title = "Authentication failed",
                Detail = result.ErrorMessage 
            });
        }

        SetRefreshTokenCookie(result.RefreshToken!);

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = 900,
            UserId = result.UserId!.Value,
            Email = result.Email!,
            Roles = result.Roles.ToList(),
            RequiresEmailConfirmation = result.RequiresEmailConfirmation
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <remarks>
    /// Use this endpoint when the access token expires.
    /// The refresh token is rotated (old one becomes invalid).
    /// Can use either request body or HTTP-only cookie.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken(
        [FromBody] RefreshTokenDto? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails 
            { 
                Title = "Refresh token required",
                Detail = "Please provide a refresh token" 
            });
        }

        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();

        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress, userAgent, cancellationToken);

        if (!result.Succeeded)
        {
            // Clear the cookie if invalid
            Response.Cookies.Delete("refreshToken");
            
            return Unauthorized(new ProblemDetails 
            { 
                Title = "Token refresh failed",
                Detail = result.ErrorMessage 
            });
        }

        SetRefreshTokenCookie(result.RefreshToken!);

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = 900,
            UserId = result.UserId!.Value,
            Email = result.Email!,
            Roles = result.Roles.ToList()
        });
    }

    /// <summary>
    /// Login with external OAuth provider
    /// </summary>
    /// <remarks>
    /// Use this after obtaining the OAuth token from the provider (Google, Facebook, etc.)
    /// The provider key should be the unique user ID from the OAuth provider.
    /// </remarks>
    [HttpPost("external-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> ExternalLogin(
        [FromBody] ExternalLoginDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();

        var result = await _authService.ExternalLoginAsync(
            new ExternalLoginRequest(
                request.Provider,
                request.ProviderKey,
                request.Email,
                ipAddress,
                userAgent,
                request.FirstName,
                request.LastName),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "External login failed",
                Detail = result.ErrorMessage 
            });
        }

        SetRefreshTokenCookie(result.RefreshToken!);

        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = 900,
            UserId = result.UserId!.Value,
            Email = result.Email!,
            Roles = result.Roles.ToList(),
            IsNewUser = result.IsNewUser
        });
    }

    /// <summary>
    /// Logout (revoke refresh token)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenDto? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        var ipAddress = GetIpAddress();

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeTokenAsync(refreshToken, ipAddress, cancellationToken);
        }

        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    /// <remarks>
    /// If the email exists, a reset token will be generated.
    /// In production, send this token via email.
    /// Always returns success to prevent email enumeration.
    /// </remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email, cancellationToken);

        // In production, don't return the token - send it via email
        return Ok(new ForgotPasswordResponse
        {
            Message = "If an account with this email exists, you will receive a password reset link.",
            // Only for demo - remove in production!
            ResetToken = result.Token
        });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(
            new ResetPasswordRequest(request.Token, request.NewPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Password reset failed",
                Detail = result.ErrorMessage 
            });
        }

        return Ok(new { message = "Password has been reset successfully" });
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ConfirmEmailAsync(request.Token, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Email confirmation failed",
                Detail = result.ErrorMessage 
            });
        }

        return Ok(new { message = "Email confirmed successfully" });
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var customerProfileId = User.FindFirst("customer_profile_id")?.Value;
        var vendorId = User.FindFirst("vendor_id")?.Value;

        return Ok(new CurrentUserResponse
        {
            UserId = Guid.Parse(userId!),
            Email = email!,
            Roles = roles,
            CustomerProfileId = customerProfileId != null ? Guid.Parse(customerProfileId) : null,
            VendorId = vendorId != null ? Guid.Parse(vendorId) : null
        });
    }

    #region Private Helpers

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString().Split(',').FirstOrDefault()?.Trim() ?? "unknown";
        }
        
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string? GetUserAgent()
    {
        return Request.Headers["User-Agent"].ToString();
    }

    #endregion
}

#region DTOs

public record RegisterDto(string Email, string Password);

public record LoginDto(string Email, string Password);

public record RefreshTokenDto(string RefreshToken);

public record ExternalLoginDto(
    string Provider,
    string ProviderKey,
    string Email,
    string? FirstName = null,
    string? LastName = null);

public record ForgotPasswordDto(string Email);

public record ResetPasswordDto(string Token, string NewPassword);

public record ConfirmEmailDto(string Token);

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool RequiresEmailConfirmation { get; set; }
    public string? EmailVerificationToken { get; set; }
    public bool IsNewUser { get; set; }
}

public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ResetToken { get; set; }
}

public class CurrentUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Guid? CustomerProfileId { get; set; }
    public Guid? VendorId { get; set; }
}

#endregion
