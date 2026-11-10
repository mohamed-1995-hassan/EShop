namespace EShop.Application.Interfaces;

/// <summary>
/// Email service interface - defined in Application, implemented in Infrastructure.
/// Supports SendGrid, Amazon SES, and other providers.
/// Demonstrates: Dependency Inversion Principle, Strategy Pattern
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a simple text email
    /// </summary>
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an email with full options
    /// </summary>
    Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a templated email
    /// </summary>
    Task<EmailSendResult> SendTemplatedEmailAsync(
        string templateId,
        string to,
        IDictionary<string, string> templateData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send bulk emails (for newsletters, notifications)
    /// </summary>
    Task<BulkEmailResult> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an email address
    /// </summary>
    Task<EmailValidationResult> ValidateEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get email delivery status (if supported by provider)
    /// </summary>
    Task<EmailDeliveryStatus?> GetDeliveryStatusAsync(
        string messageId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an email message with all options
/// </summary>
public class EmailMessage
{
    public string From { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string To { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public IList<string> Cc { get; set; } = new List<string>();
    public IList<string> Bcc { get; set; } = new List<string>();
    public string Subject { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public string? ReplyTo { get; set; }
    public IList<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public bool TrackOpens { get; set; } = true;
    public bool TrackClicks { get; set; } = true;
    public string? TemplateId { get; set; }
    public IDictionary<string, string>? TemplateData { get; set; }
    public IList<string> Tags { get; set; } = new List<string>();
    public string? Category { get; set; }
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Create a simple email message
    /// </summary>
    public static EmailMessage Create(string to, string subject, string body, bool isHtml = false)
    {
        return new EmailMessage
        {
            To = to,
            Subject = subject,
            TextBody = isHtml ? null : body,
            HtmlBody = isHtml ? body : null
        };
    }

    /// <summary>
    /// Create an HTML email message
    /// </summary>
    public static EmailMessage CreateHtml(string to, string subject, string htmlBody, string? textBody = null)
    {
        return new EmailMessage
        {
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
    }
}

/// <summary>
/// Email attachment
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string? ContentId { get; set; } // For inline attachments
    public bool IsInline { get; set; }

    public static EmailAttachment FromBytes(string fileName, byte[] content, string? contentType = null)
    {
        return new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            ContentType = contentType ?? "application/octet-stream"
        };
    }

    public static EmailAttachment FromBase64(string fileName, string base64Content, string? contentType = null)
    {
        return new EmailAttachment
        {
            FileName = fileName,
            Content = Convert.FromBase64String(base64Content),
            ContentType = contentType ?? "application/octet-stream"
        };
    }
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low,
    Normal,
    High
}

/// <summary>
/// Result of sending an email
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? ProviderResponse { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public static EmailSendResult Succeeded(string messageId, string? providerResponse = null)
    {
        return new EmailSendResult
        {
            Success = true,
            MessageId = messageId,
            ProviderResponse = providerResponse
        };
    }

    public static EmailSendResult Failed(string errorMessage, string? errorCode = null)
    {
        return new EmailSendResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Result of sending bulk emails
/// </summary>
public class BulkEmailResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public IList<EmailSendResult> Results { get; set; } = new List<EmailSendResult>();
    public IList<BulkEmailError> Errors { get; set; } = new List<BulkEmailError>();

    public static BulkEmailResult Create(IList<EmailSendResult> results)
    {
        return new BulkEmailResult
        {
            TotalCount = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success),
            Results = results,
            Errors = results.Where(r => !r.Success)
                .Select(r => new BulkEmailError { ErrorMessage = r.ErrorMessage ?? "Unknown error" })
                .ToList()
        };
    }
}

/// <summary>
/// Error in bulk email send
/// </summary>
public class BulkEmailError
{
    public string? Email { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Email validation result
/// </summary>
public class EmailValidationResult
{
    public bool IsValid { get; set; }
    public bool IsDisposable { get; set; }
    public bool IsRoleBasedAddress { get; set; }
    public bool IsFreeProvider { get; set; }
    public string? Suggestion { get; set; }
    public string? ValidationMessage { get; set; }

    public static EmailValidationResult Valid() => new() { IsValid = true };
    public static EmailValidationResult Invalid(string message) => new() { IsValid = false, ValidationMessage = message };
}

/// <summary>
/// Email delivery status
/// </summary>
public class EmailDeliveryStatus
{
    public string MessageId { get; set; } = string.Empty;
    public DeliveryState State { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public string? BounceReason { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }
}

/// <summary>
/// Email delivery states
/// </summary>
public enum DeliveryState
{
    Pending,
    Sent,
    Delivered,
    Opened,
    Clicked,
    Bounced,
    Complained,
    Unsubscribed,
    Failed
}
