using EShop.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Email;

/// <summary>
/// Fake email service for development and testing.
/// Logs emails to console instead of sending them.
/// </summary>
public class FakeEmailService : BaseEmailService
{
    private readonly List<EmailMessage> _sentEmails = new();
    private readonly object _lock = new();

    public FakeEmailService(
        IOptions<EmailConfiguration> configuration,
        ILogger<FakeEmailService> logger)
        : base(configuration, logger)
    {
    }

    /// <summary>
    /// Get all "sent" emails (for testing purposes)
    /// </summary>
    public IReadOnlyList<EmailMessage> SentEmails
    {
        get
        {
            lock (_lock)
            {
                return _sentEmails.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Clear sent emails (for testing purposes)
    /// </summary>
    public void ClearSentEmails()
    {
        lock (_lock)
        {
            _sentEmails.Clear();
        }
    }

    public override Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ApplyDefaults(message);

        // Store the email for testing
        lock (_lock)
        {
            _sentEmails.Add(message);
        }

        // Log the email
        var attachmentInfo = message.Attachments.Any()
            ? $"\n📎 Attachments: {string.Join(", ", message.Attachments.Select(a => $"{a.FileName} ({a.Content.Length} bytes)"))}"
            : "";

        var ccInfo = message.Cc.Any() ? $"\n📋 CC: {string.Join(", ", message.Cc)}" : "";
        var bccInfo = message.Bcc.Any() ? $"\n🔒 BCC: {string.Join(", ", message.Bcc)}" : "";

        Logger.LogInformation(
            """

            ═══════════════════════════════════════════════════════════════════════════════
            📨 EMAIL SENT (Development Mode - Not Actually Sent)
            ───────────────────────────────────────────────────────────────────────────────
            From:    {From} <{FromEmail}>
            To:      {To}{Cc}{Bcc}
            Subject: {Subject}
            Priority: {Priority}{Attachments}
            ───────────────────────────────────────────────────────────────────────────────
            {Body}
            ═══════════════════════════════════════════════════════════════════════════════

            """,
            message.FromName ?? "No Name",
            message.From,
            message.To,
            ccInfo,
            bccInfo,
            message.Subject,
            message.Priority,
            attachmentInfo,
            message.HtmlBody ?? message.TextBody ?? "(No body)");

        var messageId = $"fake-{Guid.NewGuid()}";
        var result = EmailSendResult.Succeeded(messageId, "Development mode - email logged but not sent");

        LogEmailSent(message, result);
        return Task.FromResult(result);
    }

    public override Task<EmailSendResult> SendTemplatedEmailAsync(
        string templateId,
        string to,
        IDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            """

            ═══════════════════════════════════════════════════════════════════════════════
            📨 TEMPLATED EMAIL SENT (Development Mode)
            ───────────────────────────────────────────────────────────────────────────────
            Template: {TemplateId}
            To:       {To}
            Data:
            {TemplateData}
            ═══════════════════════════════════════════════════════════════════════════════

            """,
            templateId,
            to,
            string.Join("\n", templateData.Select(kvp => $"  {kvp.Key}: {kvp.Value}")));

        return Task.FromResult(EmailSendResult.Succeeded(
            $"fake-template-{Guid.NewGuid()}",
            "Development mode - templated email logged but not sent"));
    }

    public override Task<BulkEmailResult> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();

        Logger.LogInformation(
            """

            ═══════════════════════════════════════════════════════════════════════════════
            📨 BULK EMAIL SENT (Development Mode)
            ───────────────────────────────────────────────────────────────────────────────
            Total Recipients: {Count}
            Recipients: {Recipients}
            ═══════════════════════════════════════════════════════════════════════════════

            """,
            messageList.Count,
            string.Join(", ", messageList.Select(m => m.To)));

        var results = messageList
            .Select(_ => EmailSendResult.Succeeded($"fake-bulk-{Guid.NewGuid()}", "Development mode"))
            .ToList();

        return Task.FromResult(BulkEmailResult.Create(results));
    }

    public override Task<EmailDeliveryStatus?> GetDeliveryStatusAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Return a fake delivered status
        return Task.FromResult<EmailDeliveryStatus?>(new EmailDeliveryStatus
        {
            MessageId = messageId,
            State = DeliveryState.Delivered,
            DeliveredAt = DateTime.UtcNow.AddSeconds(-Random.Shared.Next(1, 60))
        });
    }
}
