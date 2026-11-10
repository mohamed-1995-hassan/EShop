using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using EShop.Infrastructure.Caching;
using EShop.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace EShop.Infrastructure.Admin;

/// <summary>
/// Implementation of admin settings management service
/// </summary>
public class AdminSettingsService : IAdminSettingsService
{
    private readonly EShopDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdminSettingsService> _logger;

    // In-memory settings storage (in production, use a database table)
    private static GeneralSettingsDto _generalSettings = new()
    {
        StoreName = "EShop Demo",
        StoreDescription = "Your one-stop e-commerce platform",
        DefaultCurrency = "USD",
        DefaultLanguage = "en",
        TimeZone = "UTC",
        SupportEmail = "support@eshop.demo",
        SupportPhone = "+1-800-ESHOP"
    };

    private static PaymentSettingsDto _paymentSettings = new()
    {
        StripeEnabled = true,
        PayPalEnabled = false,
        CodEnabled = true,
        AcceptedCurrencies = new[] { "USD", "EUR", "GBP" },
        AutoCapturePayments = true
    };

    private static ShippingSettingsDto _shippingSettings = new()
    {
        FreeShippingEnabled = true,
        FreeShippingThreshold = 100,
        DefaultShippingRate = 9.99m,
        ShippingZones = new List<ShippingZoneDto>
        {
            new() { Id = "us", Name = "United States", Countries = new[] { "US" }, Rate = 9.99m },
            new() { Id = "eu", Name = "Europe", Countries = new[] { "DE", "FR", "GB", "IT", "ES" }, Rate = 19.99m },
            new() { Id = "intl", Name = "International", Countries = new[] { "*" }, Rate = 29.99m }
        }
    };

    private static TaxSettingsDto _taxSettings = new()
    {
        TaxEnabled = true,
        PricesIncludeTax = false,
        DefaultTaxRate = 0.08m,
        TaxRates = new List<TaxRateDto>
        {
            new() { Id = "us-ca", Name = "California", Country = "US", State = "CA", Rate = 0.0725m },
            new() { Id = "us-ny", Name = "New York", Country = "US", State = "NY", Rate = 0.08m },
            new() { Id = "de-vat", Name = "Germany VAT", Country = "DE", Rate = 0.19m }
        }
    };

    private static NotificationSettingsDto _notificationSettings = new()
    {
        OrderConfirmationEnabled = true,
        ShippingNotificationEnabled = true,
        DeliveryNotificationEnabled = true,
        AbandonedCartEmailEnabled = true,
        AbandonedCartDelayHours = 24,
        LowStockAlertEnabled = true,
        LowStockThreshold = 10
    };

    private static readonly List<EmailTemplateDto> _emailTemplates = new()
    {
        new() { Id = "order-confirmation", Name = "Order Confirmation", Subject = "Your Order #{OrderNumber} has been confirmed", Body = "Thank you for your order...", IsActive = true },
        new() { Id = "shipping-notification", Name = "Shipping Notification", Subject = "Your Order #{OrderNumber} has shipped", Body = "Your order is on its way...", IsActive = true },
        new() { Id = "delivery-confirmation", Name = "Delivery Confirmation", Subject = "Your Order #{OrderNumber} has been delivered", Body = "Your order has arrived...", IsActive = true },
        new() { Id = "password-reset", Name = "Password Reset", Subject = "Reset your password", Body = "Click the link to reset your password...", IsActive = true },
        new() { Id = "welcome", Name = "Welcome Email", Subject = "Welcome to {StoreName}", Body = "Thank you for joining...", IsActive = true },
        new() { Id = "abandoned-cart", Name = "Abandoned Cart", Subject = "You left something behind", Body = "Your cart is waiting...", IsActive = true }
    };

    public AdminSettingsService(
        EShopDbContext dbContext,
        ICacheService cacheService,
        ILogger<AdminSettingsService> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region General Settings

    public Task<GeneralSettingsDto> GetGeneralSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_generalSettings);
    }

    public Task<GeneralSettingsDto> UpdateGeneralSettingsAsync(GeneralSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _generalSettings = settings;
        _logger.LogInformation("General settings updated");
        return Task.FromResult(_generalSettings);
    }

    #endregion

    #region Payment Settings

    public Task<PaymentSettingsDto> GetPaymentSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_paymentSettings);
    }

    public Task<PaymentSettingsDto> UpdatePaymentSettingsAsync(PaymentSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _paymentSettings = settings;
        _logger.LogInformation("Payment settings updated");
        return Task.FromResult(_paymentSettings);
    }

    #endregion

    #region Shipping Settings

    public Task<ShippingSettingsDto> GetShippingSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_shippingSettings);
    }

    public Task<ShippingSettingsDto> UpdateShippingSettingsAsync(ShippingSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _shippingSettings = settings;
        _logger.LogInformation("Shipping settings updated");
        return Task.FromResult(_shippingSettings);
    }

    #endregion

    #region Tax Settings

    public Task<TaxSettingsDto> GetTaxSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_taxSettings);
    }

    public Task<TaxSettingsDto> UpdateTaxSettingsAsync(TaxSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _taxSettings = settings;
        _logger.LogInformation("Tax settings updated");
        return Task.FromResult(_taxSettings);
    }

    #endregion

    #region Notification Settings

    public Task<NotificationSettingsDto> GetNotificationSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_notificationSettings);
    }

    public Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(NotificationSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _notificationSettings = settings;
        _logger.LogInformation("Notification settings updated");
        return Task.FromResult(_notificationSettings);
    }

    #endregion

    #region Email Templates

    public Task<IReadOnlyList<EmailTemplateDto>> GetEmailTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<EmailTemplateDto>>(_emailTemplates);
    }

    public Task<EmailTemplateDto?> GetEmailTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        var template = _emailTemplates.FirstOrDefault(t => t.Id == templateId);
        return Task.FromResult(template);
    }

    public Task<EmailTemplateDto> UpdateEmailTemplateAsync(string templateId, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var index = _emailTemplates.FindIndex(t => t.Id == templateId);
        if (index < 0)
            throw new InvalidOperationException($"Email template {templateId} not found");

        var existingTemplate = _emailTemplates[index];
        var updatedTemplate = new EmailTemplateDto
        {
            Id = existingTemplate.Id,
            Name = existingTemplate.Name,
            Subject = request.Subject,
            Body = request.Body,
            IsActive = request.IsActive
        };

        _emailTemplates[index] = updatedTemplate;
        _logger.LogInformation("Email template {TemplateId} updated", templateId);

        return Task.FromResult(updatedTemplate);
    }

    public Task<string> PreviewEmailTemplateAsync(string templateId, IDictionary<string, string>? sampleData = null, CancellationToken cancellationToken = default)
    {
        var template = _emailTemplates.FirstOrDefault(t => t.Id == templateId);
        if (template == null)
            throw new InvalidOperationException($"Email template {templateId} not found");

        var preview = template.Body;
        var defaultData = new Dictionary<string, string>
        {
            { "StoreName", _generalSettings.StoreName },
            { "OrderNumber", "ORD-12345" },
            { "CustomerName", "John Doe" },
            { "OrderTotal", "$99.99" }
        };

        var data = sampleData ?? defaultData;
        foreach (var kvp in data)
        {
            preview = preview.Replace($"{{{kvp.Key}}}", kvp.Value);
        }

        return Task.FromResult(preview);
    }

    public Task SendTestEmailAsync(string templateId, string recipientEmail, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would send an actual email
        _logger.LogInformation("Test email sent: Template={TemplateId}, Recipient={Recipient}", templateId, recipientEmail);
        return Task.CompletedTask;
    }

    #endregion

    #region Audit Logs

    public Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        string? performedBy = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would query an audit log table
        // For now, return mock data
        var mockLogs = new List<AuditLogDto>
        {
            new() { Id = "1", Action = "Create", EntityType = "Product", EntityId = "prod-1", PerformedBy = "admin@eshop.demo", IpAddress = "192.168.1.1", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = "2", Action = "Update", EntityType = "Order", EntityId = "ord-1", PerformedBy = "admin@eshop.demo", IpAddress = "192.168.1.1", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new() { Id = "3", Action = "Delete", EntityType = "User", EntityId = "user-1", PerformedBy = "superadmin@eshop.demo", IpAddress = "192.168.1.2", Timestamp = DateTime.UtcNow.AddDays(-1) }
        };

        return Task.FromResult(PagedResult<AuditLogDto>.Create(mockLogs, mockLogs.Count, page, pageSize));
    }

    public Task<byte[]> ExportAuditLogsAsync(
        string format,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var logs = new List<AuditLogDto>
        {
            new() { Id = "1", Action = "Create", EntityType = "Product", EntityId = "prod-1", PerformedBy = "admin@eshop.demo", IpAddress = "192.168.1.1", Timestamp = DateTime.UtcNow }
        };

        if (format.ToLowerInvariant() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id,Action,EntityType,EntityId,PerformedBy,IpAddress,Timestamp");
            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id},{log.Action},{log.EntityType},{log.EntityId},{log.PerformedBy},{log.IpAddress},{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }
            return Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true })));
    }

    #endregion

    #region Cache Management

    public async Task ClearCacheAsync(string? cacheKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            // Clear all cache - would need pattern-based deletion in Redis
            _logger.LogInformation("Full cache clear requested");
        }
        else
        {
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogInformation("Cache cleared for key: {CacheKey}", cacheKey);
        }
    }

    public Task<IDictionary<string, object>> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would query Redis INFO command
        var stats = new Dictionary<string, object>
        {
            { "connected_clients", 10 },
            { "used_memory_human", "256MB" },
            { "total_keys", 1500 },
            { "hit_rate", "98.5%" },
            { "uptime_seconds", 86400 }
        };

        return Task.FromResult<IDictionary<string, object>>(stats);
    }

    #endregion
}
