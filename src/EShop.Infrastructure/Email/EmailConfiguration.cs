namespace EShop.Infrastructure.Email;

/// <summary>
/// Email service configuration
/// </summary>
public class EmailConfiguration
{
    public const string SectionName = "Email";

    /// <summary>
    /// The email provider to use: SendGrid, AmazonSES, Smtp, or Fake
    /// </summary>
    public string Provider { get; set; } = "Fake";

    /// <summary>
    /// Default sender email address
    /// </summary>
    public string DefaultFromEmail { get; set; } = "noreply@eshop.com";

    /// <summary>
    /// Default sender name
    /// </summary>
    public string DefaultFromName { get; set; } = "EShop";

    /// <summary>
    /// Enable email tracking (opens, clicks)
    /// </summary>
    public bool EnableTracking { get; set; } = true;

    /// <summary>
    /// Sandbox mode - emails are logged but not sent
    /// </summary>
    public bool SandboxMode { get; set; } = false;

    /// <summary>
    /// Maximum retry attempts for failed emails
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// SendGrid specific settings
    /// </summary>
    public SendGridSettings SendGrid { get; set; } = new();

    /// <summary>
    /// Amazon SES specific settings
    /// </summary>
    public AmazonSesSettings AmazonSes { get; set; } = new();

    /// <summary>
    /// SMTP settings (for generic SMTP providers)
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new();

    /// <summary>
    /// Template settings
    /// </summary>
    public EmailTemplateSettings Templates { get; set; } = new();
}

/// <summary>
/// SendGrid specific configuration
/// </summary>
public class SendGridSettings
{
    /// <summary>
    /// SendGrid API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Enable click tracking
    /// </summary>
    public bool EnableClickTracking { get; set; } = true;

    /// <summary>
    /// Enable open tracking
    /// </summary>
    public bool EnableOpenTracking { get; set; } = true;

    /// <summary>
    /// IP Pool name (optional)
    /// </summary>
    public string? IpPoolName { get; set; }

    /// <summary>
    /// Unsubscribe group ID (optional)
    /// </summary>
    public int? UnsubscribeGroupId { get; set; }
}

/// <summary>
/// Amazon SES specific configuration
/// </summary>
public class AmazonSesSettings
{
    /// <summary>
    /// AWS Access Key ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// AWS Secret Access Key
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS Region (e.g., us-east-1, eu-west-1)
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Configuration set name (optional, for tracking)
    /// </summary>
    public string? ConfigurationSetName { get; set; }

    /// <summary>
    /// Use SES v2 API
    /// </summary>
    public bool UseV2Api { get; set; } = true;
}

/// <summary>
/// SMTP settings for generic providers
/// </summary>
public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Email template settings
/// </summary>
public class EmailTemplateSettings
{
    /// <summary>
    /// Path to template files (for file-based templates)
    /// </summary>
    public string? TemplatePath { get; set; }

    /// <summary>
    /// Use provider templates (SendGrid dynamic templates, SES templates)
    /// </summary>
    public bool UseProviderTemplates { get; set; } = false;

    /// <summary>
    /// Default template IDs for common emails
    /// </summary>
    public EmailTemplateIds TemplateIds { get; set; } = new();
}

/// <summary>
/// Template IDs for common email types
/// </summary>
public class EmailTemplateIds
{
    public string? WelcomeEmail { get; set; }
    public string? PasswordReset { get; set; }
    public string? OrderConfirmation { get; set; }
    public string? OrderShipped { get; set; }
    public string? OrderDelivered { get; set; }
    public string? OrderCancelled { get; set; }
    public string? PaymentReceived { get; set; }
    public string? PaymentFailed { get; set; }
    public string? RefundProcessed { get; set; }
    public string? AccountVerification { get; set; }
    public string? TwoFactorCode { get; set; }
    public string? NewsletterSubscription { get; set; }
}
