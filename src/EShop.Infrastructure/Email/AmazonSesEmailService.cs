using EShop.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EShop.Infrastructure.Email;

/// <summary>
/// Amazon SES email service implementation using direct HTTP API
/// This avoids the AWS SDK dependency and uses the SES v2 API directly
/// </summary>
public class AmazonSesEmailService : BaseEmailService
{
    private readonly AmazonSesSettings _settings;
    private readonly HttpClient _httpClient;

    public AmazonSesEmailService(
        IOptions<EmailConfiguration> configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AmazonSesEmailService> logger)
        : base(configuration, logger)
    {
        _settings = configuration.Value.AmazonSes;

        if (string.IsNullOrEmpty(_settings.AccessKeyId) || string.IsNullOrEmpty(_settings.SecretAccessKey))
            throw new InvalidOperationException("AWS credentials are not configured");

        _httpClient = httpClientFactory.CreateClient("AmazonSES");
        _httpClient.BaseAddress = new Uri($"https://email.{_settings.Region}.amazonaws.com");
    }

    public override async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ApplyDefaults(message);

        try
        {
            if (Configuration.SandboxMode)
            {
                Logger.LogInformation(
                    "SES sandbox mode - Email would be sent to: {To}, Subject: {Subject}",
                    message.To, message.Subject);
                return EmailSendResult.Succeeded($"sandbox-{Guid.NewGuid()}", "Sandbox mode - not sent");
            }

            var requestBody = BuildSesRequest(message);
            var result = await SendSesRequest(requestBody, cancellationToken);

            LogEmailSent(message, result);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Amazon SES error sending email to {To}", message.To);
            return EmailSendResult.Failed(ex.Message, "SES_ERROR");
        }
    }

    public override async Task<EmailSendResult> SendTemplatedEmailAsync(
        string templateId,
        string to,
        IDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (Configuration.SandboxMode)
            {
                Logger.LogInformation(
                    "SES sandbox mode - Templated email would be sent to: {To}, Template: {Template}",
                    to, templateId);
                return EmailSendResult.Succeeded($"sandbox-{Guid.NewGuid()}", "Sandbox mode - not sent");
            }

            var requestBody = new
            {
                Content = new
                {
                    Template = new
                    {
                        TemplateName = templateId,
                        TemplateData = JsonSerializer.Serialize(templateData)
                    }
                },
                Destination = new
                {
                    ToAddresses = new[] { to }
                },
                FromEmailAddress = Configuration.DefaultFromEmail,
                ConfigurationSetName = _settings.ConfigurationSetName
            };

            var result = await SendSesRequest(requestBody, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Amazon SES error sending templated email to {To}", to);
            return EmailSendResult.Failed(ex.Message, "SES_TEMPLATE_ERROR");
        }
    }

    private object BuildSesRequest(EmailMessage message)
    {
        var content = new Dictionary<string, object>();

        // Build simple or raw message
        if (message.Attachments.Any())
        {
            // Use raw message for attachments
            content["Raw"] = new
            {
                Data = Convert.ToBase64String(BuildRawMessage(message))
            };
        }
        else
        {
            // Use simple message
            var body = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(message.HtmlBody))
            {
                body["Html"] = new { Charset = "UTF-8", Data = message.HtmlBody };
            }

            if (!string.IsNullOrEmpty(message.TextBody))
            {
                body["Text"] = new { Charset = "UTF-8", Data = message.TextBody };
            }

            content["Simple"] = new
            {
                Body = body,
                Subject = new { Charset = "UTF-8", Data = message.Subject }
            };
        }

        var destination = new Dictionary<string, object>
        {
            ["ToAddresses"] = new[] { message.To }
        };

        if (message.Cc.Any())
        {
            destination["CcAddresses"] = message.Cc.ToArray();
        }

        if (message.Bcc.Any())
        {
            destination["BccAddresses"] = message.Bcc.ToArray();
        }

        var request = new Dictionary<string, object>
        {
            ["Content"] = content,
            ["Destination"] = destination,
            ["FromEmailAddress"] = string.IsNullOrEmpty(message.FromName)
                ? message.From
                : $"{message.FromName} <{message.From}>"
        };

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            request["ReplyToAddresses"] = new[] { message.ReplyTo };
        }

        if (!string.IsNullOrEmpty(_settings.ConfigurationSetName))
        {
            request["ConfigurationSetName"] = _settings.ConfigurationSetName;
        }

        // Add tags
        if (message.Tags.Any() || message.Metadata.Any())
        {
            var tags = message.Tags.Select(t => new { Name = "tag", Value = t }).ToList();
            tags.AddRange(message.Metadata.Select(m => new { Name = m.Key, Value = m.Value }));
            request["EmailTags"] = tags;
        }

        return request;
    }

    private byte[] BuildRawMessage(EmailMessage message)
    {
        var boundary = $"----=_Part_{Guid.NewGuid():N}";
        var sb = new StringBuilder();

        // Headers
        sb.AppendLine($"From: {(string.IsNullOrEmpty(message.FromName) ? message.From : $"{message.FromName} <{message.From}>")}");
        sb.AppendLine($"To: {message.To}");

        if (message.Cc.Any())
            sb.AppendLine($"Cc: {string.Join(", ", message.Cc)}");

        if (!string.IsNullOrEmpty(message.ReplyTo))
            sb.AppendLine($"Reply-To: {message.ReplyTo}");

        sb.AppendLine($"Subject: {message.Subject}");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        sb.AppendLine();

        // Text body
        if (!string.IsNullOrEmpty(message.TextBody))
        {
            sb.AppendLine($"--{boundary}");
            sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
            sb.AppendLine("Content-Transfer-Encoding: 7bit");
            sb.AppendLine();
            sb.AppendLine(message.TextBody);
        }

        // HTML body
        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            sb.AppendLine($"--{boundary}");
            sb.AppendLine("Content-Type: text/html; charset=UTF-8");
            sb.AppendLine("Content-Transfer-Encoding: 7bit");
            sb.AppendLine();
            sb.AppendLine(message.HtmlBody);
        }

        // Attachments
        foreach (var attachment in message.Attachments)
        {
            sb.AppendLine($"--{boundary}");
            sb.AppendLine($"Content-Type: {attachment.ContentType}; name=\"{attachment.FileName}\"");
            sb.AppendLine("Content-Transfer-Encoding: base64");
            
            if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
            {
                sb.AppendLine($"Content-ID: <{attachment.ContentId}>");
                sb.AppendLine("Content-Disposition: inline");
            }
            else
            {
                sb.AppendLine($"Content-Disposition: attachment; filename=\"{attachment.FileName}\"");
            }
            
            sb.AppendLine();
            sb.AppendLine(Convert.ToBase64String(attachment.Content));
        }

        sb.AppendLine($"--{boundary}--");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<EmailSendResult> SendSesRequest(object requestBody, CancellationToken cancellationToken)
    {
        var endpoint = _settings.UseV2Api ? "/v2/email/outbound-emails" : "/";
        var jsonContent = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        // Sign the request with AWS Signature Version 4
        SignRequest(request, jsonContent);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var messageId = doc.RootElement.GetProperty("MessageId").GetString();
                return EmailSendResult.Succeeded(messageId ?? Guid.NewGuid().ToString(), responseBody);
            }
            catch
            {
                return EmailSendResult.Succeeded(Guid.NewGuid().ToString(), responseBody);
            }
        }

        Logger.LogWarning(
            "Amazon SES API error. Status: {StatusCode}, Response: {Response}",
            response.StatusCode, responseBody);

        return EmailSendResult.Failed(
            $"SES API error: {response.StatusCode} - {responseBody}",
            response.StatusCode.ToString());
    }

    private void SignRequest(HttpRequestMessage request, string payload)
    {
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");

        var host = $"email.{_settings.Region}.amazonaws.com";
        var service = "ses";
        var algorithm = "AWS4-HMAC-SHA256";

        // Create canonical request
        var payloadHash = ComputeSha256Hash(payload);
        var canonicalHeaders = $"host:{host}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-date";

        var canonicalRequest = $"POST\n{request.RequestUri?.PathAndQuery ?? "/"}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        // Create string to sign
        var credentialScope = $"{dateStamp}/{_settings.Region}/{service}/aws4_request";
        var stringToSign = $"{algorithm}\n{amzDate}\n{credentialScope}\n{ComputeSha256Hash(canonicalRequest)}";

        // Calculate signing key
        var signingKey = GetSignatureKey(_settings.SecretAccessKey, dateStamp, _settings.Region, service);

        // Calculate signature
        var signature = ToHexString(HmacSha256(signingKey, stringToSign));

        // Add authorization header
        var authorizationHeader = $"{algorithm} Credential={_settings.AccessKeyId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        request.Headers.Add("X-Amz-Date", amzDate);
        request.Headers.Authorization = new AuthenticationHeaderValue("AWS4-HMAC-SHA256", authorizationHeader.Replace("AWS4-HMAC-SHA256 ", ""));
        request.Headers.Host = host;
    }

    private static string ComputeSha256Hash(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return ToHexString(bytes);
    }

    private static string ToHexString(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static byte[] GetSignatureKey(string secretKey, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        var kSigning = HmacSha256(kService, "aws4_request");
        return kSigning;
    }
}
