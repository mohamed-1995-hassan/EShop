using EShop.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace EShop.Infrastructure.Email;

/// <summary>
/// SMTP email service implementation for generic email providers
/// </summary>
public class SmtpEmailService : BaseEmailService
{
    private readonly SmtpSettings _settings;

    public SmtpEmailService(
        IOptions<EmailConfiguration> configuration,
        ILogger<SmtpEmailService> logger)
        : base(configuration, logger)
    {
        _settings = configuration.Value.Smtp;
    }

    public override async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ApplyDefaults(message);

        if (Configuration.SandboxMode)
        {
            Logger.LogInformation(
                "SMTP sandbox mode - Email would be sent to: {To}, Subject: {Subject}",
                message.To, message.Subject);
            return EmailSendResult.Succeeded($"sandbox-{Guid.NewGuid()}", "Sandbox mode - not sent");
        }

        try
        {
            using var client = CreateSmtpClient();
            using var mailMessage = BuildMailMessage(message);

            await client.SendMailAsync(mailMessage, cancellationToken);

            var messageId = Guid.NewGuid().ToString();
            var result = EmailSendResult.Succeeded(messageId);

            LogEmailSent(message, result);
            return result;
        }
        catch (SmtpException ex)
        {
            Logger.LogError(ex, "SMTP error sending email to {To}", message.To);
            return EmailSendResult.Failed(ex.Message, ex.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending email via SMTP to {To}", message.To);
            return EmailSendResult.Failed(ex.Message, "SMTP_ERROR");
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Timeout = _settings.TimeoutSeconds * 1000
        };

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        return client;
    }

    private MailMessage BuildMailMessage(EmailMessage message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(message.From, message.FromName),
            Subject = message.Subject,
            IsBodyHtml = !string.IsNullOrEmpty(message.HtmlBody)
        };

        // Set body
        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            mailMessage.Body = message.HtmlBody;

            // Add plain text as alternate view
            if (!string.IsNullOrEmpty(message.TextBody))
            {
                var plainView = AlternateView.CreateAlternateViewFromString(
                    message.TextBody,
                    null,
                    "text/plain");
                mailMessage.AlternateViews.Add(plainView);
            }
        }
        else if (!string.IsNullOrEmpty(message.TextBody))
        {
            mailMessage.Body = message.TextBody;
        }

        // Add recipient
        mailMessage.To.Add(new MailAddress(message.To, message.ToName));

        // Add CC
        foreach (var cc in message.Cc)
        {
            mailMessage.CC.Add(new MailAddress(cc));
        }

        // Add BCC
        foreach (var bcc in message.Bcc)
        {
            mailMessage.Bcc.Add(new MailAddress(bcc));
        }

        // Reply-To
        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            mailMessage.ReplyToList.Add(new MailAddress(message.ReplyTo));
        }

        // Priority
        mailMessage.Priority = message.Priority switch
        {
            EmailPriority.High => MailPriority.High,
            EmailPriority.Low => MailPriority.Low,
            _ => MailPriority.Normal
        };

        // Attachments
        foreach (var attachment in message.Attachments)
        {
            var stream = new MemoryStream(attachment.Content);
            var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);

            if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
            {
                mailAttachment.ContentId = attachment.ContentId;
                mailAttachment.ContentDisposition!.Inline = true;
                mailAttachment.ContentDisposition.DispositionType = "inline";
            }

            mailMessage.Attachments.Add(mailAttachment);
        }

        // Custom headers
        foreach (var header in message.Headers)
        {
            mailMessage.Headers.Add(header.Key, header.Value);
        }

        return mailMessage;
    }
}
