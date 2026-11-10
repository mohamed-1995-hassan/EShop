namespace EShop.Infrastructure.Identity;

/// <summary>
/// JWT Configuration Settings
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing tokens (min 32 characters)
    /// In production, use Azure Key Vault or AWS Secrets Manager
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (your API URL)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (your frontend URLs)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in minutes (default: 15)
    /// Keep short for security
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration in days (default: 7)
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Allow refresh token rotation (recommended: true)
    /// </summary>
    public bool RotateRefreshTokens { get; set; } = true;

    /// <summary>
    /// Clock skew for token validation (default: 0)
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 0;
}

/// <summary>
/// Password policy settings
/// </summary>
public class PasswordSettings
{
    public const string SectionName = "Password";

    public int MinimumLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}

/// <summary>
/// OAuth provider settings
/// </summary>
public class OAuthSettings
{
    public const string SectionName = "OAuth";

    public GoogleSettings? Google { get; set; }
    public FacebookSettings? Facebook { get; set; }
    public AppleSettings? Apple { get; set; }
    public MicrosoftSettings? Microsoft { get; set; }
}

public class GoogleSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class FacebookSettings
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}

public class AppleSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

public class MicrosoftSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}
