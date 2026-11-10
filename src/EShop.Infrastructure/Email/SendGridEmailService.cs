using EShop.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace EShop.Infrastructure.Email;

/// <summary>
/// SendGrid email service implementation
/// </summary>
public class SendGridEmailService : BaseEmailService
{
    private readonly ISendGridClient _client;
    private readonly SendGridSettings _settings;

    public SendGridEmailService(
        IOptions<EmailConfiguration> configuration,
        ILogger<SendGridEmailService> logger)
        : base(configuration, logger)
    {
        _settings = configuration.Value.SendGrid;

        if (string.IsNullOrEmpty(_settings.ApiKey))
            throw new InvalidOperationException("SendGrid API key is not configured");

        _client = new SendGridClient(_settings.ApiKey);
    }

    public override async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ApplyDefaults(message);

        try
        {
            var sendGridMessage = BuildSendGridMessage(message);

            // Handle sandbox mode
            if (Configuration.SandboxMode)
            {
                sendGridMessage.SetSandBoxMode(true);
                Logger.LogInformation("SendGrid sandbox mode enabled - email will not be delivered");
            }

            var response = await _client.SendEmailAsync(sendGridMessage, cancellationToken);

            var result = await ProcessResponse(response, message);
            LogEmailSent(message, result);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SendGrid error sending email to {To}", message.To);
            return EmailSendResult.Failed(ex.Message, "SENDGRID_ERROR");
        }
    }

    public override async Task<EmailSendResult> SendTemplatedEmailAsync(
        string templateId,
        string to,
        IDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        ApplyDefaults(new EmailMessage { To = to });

        try
        {
            var message = new SendGridMessage
            {
                From = new EmailAddress(Configuration.DefaultFromEmail, Configuration.DefaultFromName),
                TemplateId = templateId
            };

            message.AddTo(new EmailAddress(to));

            // Add template data as dynamic template data
            var dynamicData = templateData.ToDictionary(k => k.Key, v => (object)v.Value);
            message.SetTemplateData(dynamicData);

            if (Configuration.SandboxMode)
            {
                message.SetSandBoxMode(true);
            }

            var response = await _client.SendEmailAsync(message, cancellationToken);

            return await ProcessResponse(response, new EmailMessage { To = to, Subject = $"Template: {templateId}" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SendGrid error sending templated email to {To}", to);
            return EmailSendResult.Failed(ex.Message, "SENDGRID_TEMPLATE_ERROR");
        }
    }

    public override async Task<BulkEmailResult> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var results = new List<EmailSendResult>();

        // SendGrid supports personalization for bulk sending
        // Group messages by subject/body for optimization
        var grouped = messageList.GroupBy(m => new { m.Subject, m.HtmlBody, m.TextBody });

        foreach (var group in grouped)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var firstMessage = group.First();
                ApplyDefaults(firstMessage);

                var sendGridMessage = new SendGridMessage
                {
                    From = new EmailAddress(firstMessage.From, firstMessage.FromName),
                    Subject = firstMessage.Subject
                };

                if (!string.IsNullOrEmpty(firstMessage.HtmlBody))
                    sendGridMessage.HtmlContent = firstMessage.HtmlBody;

                if (!string.IsNullOrEmpty(firstMessage.TextBody))
                    sendGridMessage.PlainTextContent = firstMessage.TextBody;

                // Add all recipients as personalizations
                foreach (var msg in group)
                {
                    sendGridMessage.AddTo(new EmailAddress(msg.To, msg.ToName));
                }

                if (Configuration.SandboxMode)
                {
                    sendGridMessage.SetSandBoxMode(true);
                }

                var response = await _client.SendEmailAsync(sendGridMessage, cancellationToken);

                var result = await ProcessResponse(response, firstMessage);

                // Add result for each recipient
                foreach (var _ in group)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SendGrid bulk send error");
                foreach (var msg in group)
                {
                    results.Add(EmailSendResult.Failed(ex.Message, "SENDGRID_BULK_ERROR"));
                }
            }
        }

        return BulkEmailResult.Create(results);
    }

    private SendGridMessage BuildSendGridMessage(EmailMessage message)
    {
        var sendGridMessage = new SendGridMessage
        {
            From = new EmailAddress(message.From, message.FromName),
            Subject = message.Subject
        };

        // Add recipient
        sendGridMessage.AddTo(new EmailAddress(message.To, message.ToName));

        // Add CC
        foreach (var cc in message.Cc)
        {
            sendGridMessage.AddCc(new EmailAddress(cc));
        }

        // Add BCC
        foreach (var bcc in message.Bcc)
        {
            sendGridMessage.AddBcc(new EmailAddress(bcc));
        }

        // Set content
        if (!string.IsNullOrEmpty(message.HtmlBody))
            sendGridMessage.HtmlContent = message.HtmlBody;

        if (!string.IsNullOrEmpty(message.TextBody))
            sendGridMessage.PlainTextContent = message.TextBody;

        // Reply-To
        if (!string.IsNullOrEmpty(message.ReplyTo))
            sendGridMessage.ReplyTo = new EmailAddress(message.ReplyTo);

        // Attachments
        foreach (var attachment in message.Attachments)
        {
            sendGridMessage.AddAttachment(
                attachment.FileName,
                Convert.ToBase64String(attachment.Content),
                attachment.ContentType,
                attachment.IsInline ? "inline" : "attachment",
                attachment.ContentId);
        }

        // Custom headers
        foreach (var header in message.Headers)
        {
            sendGridMessage.AddHeader(header.Key, header.Value);
        }

        // Tracking settings
        if (_settings.EnableClickTracking)
        {
            sendGridMessage.SetClickTracking(message.TrackClicks, message.TrackClicks);
        }

        if (_settings.EnableOpenTracking)
        {
            sendGridMessage.SetOpenTracking(message.TrackOpens);
        }

        // Categories and tags
        if (!string.IsNullOrEmpty(message.Category))
        {
            sendGridMessage.AddCategory(message.Category);
        }

        foreach (var tag in message.Tags)
        {
            sendGridMessage.AddCategory(tag);
        }

        // Custom args (metadata)
        foreach (var metadata in message.Metadata)
        {
            sendGridMessage.AddCustomArg(metadata.Key, metadata.Value);
        }

        // IP Pool
        if (!string.IsNullOrEmpty(_settings.IpPoolName))
        {
            sendGridMessage.SetIpPoolName(_settings.IpPoolName);
        }

        // Unsubscribe group
        if (_settings.UnsubscribeGroupId.HasValue)
        {
            sendGridMessage.SetAsm(_settings.UnsubscribeGroupId.Value);
        }

        // Scheduled sending
        if (message.ScheduledAt.HasValue)
        {
            var unixTime = ((DateTimeOffset)message.ScheduledAt.Value).ToUnixTimeSeconds();
            sendGridMessage.SendAt = unixTime;
        }

        return sendGridMessage;
    }

    private async Task<EmailSendResult> ProcessResponse(Response response, EmailMessage message)
    {
        var body = await response.Body.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.Accepted ||
            response.StatusCode == HttpStatusCode.Created)
        {
            // Extract message ID from headers
            var messageId = response.Headers.TryGetValues("X-Message-Id", out var values) 
                ? values.FirstOrDefault() 
                : Guid.NewGuid().ToString();

            return EmailSendResult.Succeeded(messageId ?? Guid.NewGuid().ToString(), body);
        }

        Logger.LogWarning(
            "SendGrid API error. Status: {StatusCode}, Response: {Response}",
            response.StatusCode, body);

        return EmailSendResult.Failed(
            $"SendGrid API error: {response.StatusCode}",
            response.StatusCode.ToString());
    }
}
