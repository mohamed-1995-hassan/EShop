using EShop.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace EShop.Infrastructure.Email;

/// <summary>
/// Service for managing and rendering email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Render a template with the given data
    /// </summary>
    Task<(string Subject, string HtmlBody, string? TextBody)> RenderTemplateAsync(
        string templateId,
        IDictionary<string, string> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a list of available templates
    /// </summary>
    Task<IReadOnlyList<EmailTemplateInfo>> GetAvailableTemplatesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an email template
/// </summary>
public class EmailTemplateInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<string> RequiredVariables { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Email template service implementation with built-in templates
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly EmailConfiguration _configuration;
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly Dictionary<string, EmailTemplate> _templates;

    public EmailTemplateService(
        IOptions<EmailConfiguration> configuration,
        ILogger<EmailTemplateService> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
        _templates = InitializeBuiltInTemplates();
    }

    public Task<(string Subject, string HtmlBody, string? TextBody)> RenderTemplateAsync(
        string templateId,
        IDictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        if (!_templates.TryGetValue(templateId, out var template))
        {
            throw new InvalidOperationException($"Email template '{templateId}' not found");
        }

        var subject = RenderContent(template.Subject, data);
        var htmlBody = RenderContent(template.HtmlBody, data);
        var textBody = template.TextBody != null ? RenderContent(template.TextBody, data) : null;

        return Task.FromResult((subject, htmlBody, textBody));
    }

    public Task<IReadOnlyList<EmailTemplateInfo>> GetAvailableTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var templates = _templates.Values
            .Select(t => new EmailTemplateInfo
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                RequiredVariables = ExtractVariables(t.HtmlBody).ToList()
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<EmailTemplateInfo>>(templates);
    }

    private static string RenderContent(string template, IDictionary<string, string> data)
    {
        var result = template;

        foreach (var kvp in data)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            result = result.Replace($"{{{{ {kvp.Key} }}}}", kvp.Value);
        }

        // Remove any remaining unmatched placeholders
        result = Regex.Replace(result, @"\{\{\s*\w+\s*\}\}", "");

        return result;
    }

    private static IEnumerable<string> ExtractVariables(string template)
    {
        var matches = Regex.Matches(template, @"\{\{\s*(\w+)\s*\}\}");
        return matches.Select(m => m.Groups[1].Value).Distinct();
    }

    private Dictionary<string, EmailTemplate> InitializeBuiltInTemplates()
    {
        return new Dictionary<string, EmailTemplate>
        {
            ["welcome"] = new EmailTemplate
            {
                Id = "welcome",
                Name = "Welcome Email",
                Description = "Sent when a new user registers",
                Subject = "Welcome to {{StoreName}}, {{CustomerName}}!",
                HtmlBody = GetWelcomeEmailHtml(),
                TextBody = GetWelcomeEmailText()
            },
            ["password-reset"] = new EmailTemplate
            {
                Id = "password-reset",
                Name = "Password Reset",
                Description = "Sent when user requests password reset",
                Subject = "Reset your {{StoreName}} password",
                HtmlBody = GetPasswordResetHtml(),
                TextBody = GetPasswordResetText()
            },
            ["order-confirmation"] = new EmailTemplate
            {
                Id = "order-confirmation",
                Name = "Order Confirmation",
                Description = "Sent when an order is placed",
                Subject = "Order Confirmed - #{{OrderNumber}}",
                HtmlBody = GetOrderConfirmationHtml(),
                TextBody = GetOrderConfirmationText()
            },
            ["order-shipped"] = new EmailTemplate
            {
                Id = "order-shipped",
                Name = "Order Shipped",
                Description = "Sent when an order is shipped",
                Subject = "Your order #{{OrderNumber}} has shipped!",
                HtmlBody = GetOrderShippedHtml(),
                TextBody = GetOrderShippedText()
            },
            ["payment-receipt"] = new EmailTemplate
            {
                Id = "payment-receipt",
                Name = "Payment Receipt",
                Description = "Sent when payment is received",
                Subject = "Payment Receipt - {{StoreName}}",
                HtmlBody = GetPaymentReceiptHtml(),
                TextBody = GetPaymentReceiptText()
            },
            ["account-verification"] = new EmailTemplate
            {
                Id = "account-verification",
                Name = "Account Verification",
                Description = "Sent to verify email address",
                Subject = "Verify your {{StoreName}} account",
                HtmlBody = GetAccountVerificationHtml(),
                TextBody = GetAccountVerificationText()
            },
            ["two-factor-code"] = new EmailTemplate
            {
                Id = "two-factor-code",
                Name = "Two-Factor Authentication Code",
                Description = "Sent with 2FA code",
                Subject = "Your {{StoreName}} verification code",
                HtmlBody = GetTwoFactorCodeHtml(),
                TextBody = GetTwoFactorCodeText()
            }
        };
    }

    #region Email Templates HTML

    private static string GetBaseHtmlTemplate(string content) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{{{{Subject}}}}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .card {{ background: #fff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); padding: 30px; margin: 20px 0; }}
        .header {{ text-align: center; padding: 20px 0; }}
        .header h1 {{ color: #2563eb; margin: 0; }}
        .button {{ display: inline-block; background: #2563eb; color: #fff !important; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .button:hover {{ background: #1d4ed8; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .order-item {{ border-bottom: 1px solid #eee; padding: 15px 0; }}
        .order-total {{ font-size: 18px; font-weight: bold; color: #2563eb; }}
        .code {{ font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #2563eb; text-align: center; padding: 20px; background: #f0f9ff; border-radius: 8px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{{{{StoreName}}}}</h1>
        </div>
        <div class=""card"">
            {content}
        </div>
        <div class=""footer"">
            <p>&copy; {{{{Year}}}} {{{{StoreName}}}}. All rights reserved.</p>
            <p>{{{{StoreAddress}}}}</p>
        </div>
    </div>
</body>
</html>";

    private static string GetWelcomeEmailHtml() => GetBaseHtmlTemplate(@"
<h2>Welcome, {{CustomerName}}!</h2>
<p>Thank you for joining {{StoreName}}. We're excited to have you as part of our community.</p>
<p>Here's what you can do next:</p>
<ul>
    <li>Browse our latest products</li>
    <li>Set up your profile</li>
    <li>Check out exclusive member deals</li>
</ul>
<p style=""text-align: center;"">
    <a href=""{{ShopUrl}}"" class=""button"">Start Shopping</a>
</p>
<p>If you have any questions, our support team is always here to help.</p>
<p>Happy shopping!<br>The {{StoreName}} Team</p>");

    private static string GetPasswordResetHtml() => GetBaseHtmlTemplate(@"
<h2>Password Reset Request</h2>
<p>Hi {{CustomerName}},</p>
<p>We received a request to reset your password. Click the button below to create a new password:</p>
<p style=""text-align: center;"">
    <a href=""{{ResetUrl}}"" class=""button"">Reset Password</a>
</p>
<p>This link will expire in {{ExpiryHours}} hours.</p>
<p><strong>Didn't request this?</strong> You can safely ignore this email. Your password won't be changed.</p>
<p>For security, this request was received from:</p>
<ul>
    <li>IP Address: {{IpAddress}}</li>
    <li>Time: {{RequestTime}}</li>
</ul>");

    private static string GetOrderConfirmationHtml() => GetBaseHtmlTemplate(@"
<h2>Order Confirmed! 🎉</h2>
<p>Hi {{CustomerName}},</p>
<p>Thank you for your order! We're getting it ready for you.</p>
<div style=""background: #f0f9ff; padding: 15px; border-radius: 8px; margin: 20px 0;"">
    <strong>Order Number:</strong> #{{OrderNumber}}<br>
    <strong>Order Date:</strong> {{OrderDate}}
</div>
<h3>Order Summary</h3>
{{OrderItems}}
<div style=""text-align: right; padding-top: 15px; border-top: 2px solid #2563eb;"">
    <p>Subtotal: {{Subtotal}}</p>
    <p>Shipping: {{ShippingCost}}</p>
    <p>Tax: {{Tax}}</p>
    <p class=""order-total"">Total: {{Total}}</p>
</div>
<h3>Shipping Address</h3>
<p>{{ShippingAddress}}</p>
<p style=""text-align: center;"">
    <a href=""{{OrderUrl}}"" class=""button"">View Order</a>
</p>");

    private static string GetOrderShippedHtml() => GetBaseHtmlTemplate(@"
<h2>Your Order Has Shipped! 📦</h2>
<p>Hi {{CustomerName}},</p>
<p>Great news! Your order #{{OrderNumber}} is on its way.</p>
<div style=""background: #f0f9ff; padding: 15px; border-radius: 8px; margin: 20px 0;"">
    <strong>Tracking Number:</strong> {{TrackingNumber}}<br>
    <strong>Carrier:</strong> {{Carrier}}<br>
    <strong>Estimated Delivery:</strong> {{EstimatedDelivery}}
</div>
<p style=""text-align: center;"">
    <a href=""{{TrackingUrl}}"" class=""button"">Track Your Package</a>
</p>
<h3>What's in your package:</h3>
{{OrderItems}}
<p>If you have any questions about your delivery, please contact us.</p>");

    private static string GetPaymentReceiptHtml() => GetBaseHtmlTemplate(@"
<h2>Payment Receipt</h2>
<p>Hi {{CustomerName}},</p>
<p>We've received your payment. Here are the details:</p>
<div style=""background: #f0f9ff; padding: 15px; border-radius: 8px; margin: 20px 0;"">
    <strong>Amount:</strong> {{Amount}}<br>
    <strong>Payment Method:</strong> {{PaymentMethod}}<br>
    <strong>Transaction ID:</strong> {{TransactionId}}<br>
    <strong>Date:</strong> {{PaymentDate}}
</div>
<p>Thank you for your purchase!</p>");

    private static string GetAccountVerificationHtml() => GetBaseHtmlTemplate(@"
<h2>Verify Your Email Address</h2>
<p>Hi {{CustomerName}},</p>
<p>Please verify your email address to complete your registration.</p>
<p style=""text-align: center;"">
    <a href=""{{VerificationUrl}}"" class=""button"">Verify Email</a>
</p>
<p>Or copy and paste this link into your browser:</p>
<p style=""word-break: break-all; color: #666;"">{{VerificationUrl}}</p>
<p>This link will expire in {{ExpiryHours}} hours.</p>");

    private static string GetTwoFactorCodeHtml() => GetBaseHtmlTemplate(@"
<h2>Your Verification Code</h2>
<p>Hi {{CustomerName}},</p>
<p>Use the following code to complete your sign-in:</p>
<div class=""code"">{{Code}}</div>
<p style=""text-align: center; margin-top: 20px;"">This code expires in {{ExpiryMinutes}} minutes.</p>
<p><strong>Didn't request this code?</strong> If you didn't try to sign in, someone may be trying to access your account. Please change your password immediately.</p>");

    #endregion

    #region Email Templates Text

    private static string GetWelcomeEmailText() => @"
Welcome, {{CustomerName}}!

Thank you for joining {{StoreName}}. We're excited to have you as part of our community.

Here's what you can do next:
- Browse our latest products
- Set up your profile  
- Check out exclusive member deals

Start shopping: {{ShopUrl}}

If you have any questions, our support team is always here to help.

Happy shopping!
The {{StoreName}} Team";

    private static string GetPasswordResetText() => @"
Password Reset Request

Hi {{CustomerName}},

We received a request to reset your password. Click the link below to create a new password:

{{ResetUrl}}

This link will expire in {{ExpiryHours}} hours.

Didn't request this? You can safely ignore this email. Your password won't be changed.";

    private static string GetOrderConfirmationText() => @"
Order Confirmed!

Hi {{CustomerName}},

Thank you for your order! We're getting it ready for you.

Order Number: #{{OrderNumber}}
Order Date: {{OrderDate}}

Total: {{Total}}

View your order: {{OrderUrl}}";

    private static string GetOrderShippedText() => @"
Your Order Has Shipped!

Hi {{CustomerName}},

Great news! Your order #{{OrderNumber}} is on its way.

Tracking Number: {{TrackingNumber}}
Carrier: {{Carrier}}
Estimated Delivery: {{EstimatedDelivery}}

Track your package: {{TrackingUrl}}";

    private static string GetPaymentReceiptText() => @"
Payment Receipt

Hi {{CustomerName}},

We've received your payment.

Amount: {{Amount}}
Payment Method: {{PaymentMethod}}
Transaction ID: {{TransactionId}}
Date: {{PaymentDate}}

Thank you for your purchase!";

    private static string GetAccountVerificationText() => @"
Verify Your Email Address

Hi {{CustomerName}},

Please verify your email address to complete your registration.

Verification link: {{VerificationUrl}}

This link will expire in {{ExpiryHours}} hours.";

    private static string GetTwoFactorCodeText() => @"
Your Verification Code

Hi {{CustomerName}},

Use the following code to complete your sign-in:

{{Code}}

This code expires in {{ExpiryMinutes}} minutes.

Didn't request this code? If you didn't try to sign in, someone may be trying to access your account.";

    #endregion
}

internal class EmailTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
}
