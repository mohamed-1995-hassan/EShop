using EShop.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Email;

/// <summary>
/// Base email service with common functionality
/// </summary>
public abstract class BaseEmailService : IEmailService
{
    protected readonly EmailConfiguration Configuration;
    protected readonly ILogger Logger;

    protected BaseEmailService(
        IOptions<EmailConfiguration> configuration,
        ILogger logger)
    {
        Configuration = configuration.Value;
        Logger = logger;
    }

    public virtual async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var message = EmailMessage.Create(to, subject, body, isHtml: body.Contains("<"));
        message.From = Configuration.DefaultFromEmail;
        message.FromName = Configuration.DefaultFromName;

        await SendAsync(message, cancellationToken);
    }

    public abstract Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);

    public virtual async Task<EmailSendResult> SendTemplatedEmailAsync(
        string templateId,
        string to,
        IDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            To = to,
            From = Configuration.DefaultFromEmail,
            FromName = Configuration.DefaultFromName,
            TemplateId = templateId,
            TemplateData = templateData
        };

        return await SendAsync(message, cancellationToken);
    }

    public virtual async Task<BulkEmailResult> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EmailSendResult>();

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var result = await SendAsync(message, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(EmailSendResult.Failed(ex.Message));
            }
        }

        return BulkEmailResult.Create(results);
    }

    public virtual Task<EmailValidationResult> ValidateEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        // Basic validation - providers can override with more sophisticated validation
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(EmailValidationResult.Invalid("Email address is required"));

        if (!email.Contains('@') || !email.Contains('.'))
            return Task.FromResult(EmailValidationResult.Invalid("Invalid email format"));

        // Check for disposable email domains (basic list)
        var disposableDomains = new[] { "mailinator.com", "guerrillamail.com", "10minutemail.com", "tempmail.com" };
        var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
        
        var result = new EmailValidationResult
        {
            IsValid = true,
            IsDisposable = disposableDomains.Contains(domain),
            IsRoleBasedAddress = IsRoleBasedEmail(email),
            IsFreeProvider = IsFreeEmailProvider(domain)
        };

        return Task.FromResult(result);
    }

    public virtual Task<EmailDeliveryStatus?> GetDeliveryStatusAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Default implementation - providers can override
        return Task.FromResult<EmailDeliveryStatus?>(null);
    }

    protected virtual void ApplyDefaults(EmailMessage message)
    {
        if (string.IsNullOrEmpty(message.From))
            message.From = Configuration.DefaultFromEmail;

        if (string.IsNullOrEmpty(message.FromName))
            message.FromName = Configuration.DefaultFromName;

        if (Configuration.EnableTracking)
        {
            message.TrackOpens = true;
            message.TrackClicks = true;
        }
    }

    protected virtual void LogEmailSent(EmailMessage message, EmailSendResult result)
    {
        if (result.Success)
        {
            Logger.LogInformation(
                "Email sent successfully. To: {To}, Subject: {Subject}, MessageId: {MessageId}",
                message.To, message.Subject, result.MessageId);
        }
        else
        {
            Logger.LogWarning(
                "Email send failed. To: {To}, Subject: {Subject}, Error: {Error}",
                message.To, message.Subject, result.ErrorMessage);
        }
    }

    private static bool IsRoleBasedEmail(string email)
    {
        var roleBasedPrefixes = new[] 
        { 
            "admin", "support", "info", "contact", "sales", "help", 
            "marketing", "webmaster", "postmaster", "noreply", "no-reply" 
        };
        var localPart = email.Split('@').FirstOrDefault()?.ToLowerInvariant();
        return roleBasedPrefixes.Contains(localPart);
    }

    private static bool IsFreeEmailProvider(string? domain)
    {
        if (string.IsNullOrEmpty(domain)) return false;
        
        var freeProviders = new[] 
        { 
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", 
            "aol.com", "icloud.com", "mail.com", "protonmail.com" 
        };
        return freeProviders.Contains(domain);
    }
}
