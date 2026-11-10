using EShop.Application.DTOs;

namespace EShop.Application.Interfaces.Admin;

/// <summary>
/// Service interface for admin settings management operations
/// </summary>
public interface IAdminSettingsService
{
    // General Settings
    Task<GeneralSettingsDto> GetGeneralSettingsAsync(CancellationToken cancellationToken = default);
    Task<GeneralSettingsDto> UpdateGeneralSettingsAsync(GeneralSettingsDto settings, CancellationToken cancellationToken = default);
    
    // Payment Settings
    Task<PaymentSettingsDto> GetPaymentSettingsAsync(CancellationToken cancellationToken = default);
    Task<PaymentSettingsDto> UpdatePaymentSettingsAsync(PaymentSettingsDto settings, CancellationToken cancellationToken = default);
    
    // Shipping Settings
    Task<ShippingSettingsDto> GetShippingSettingsAsync(CancellationToken cancellationToken = default);
    Task<ShippingSettingsDto> UpdateShippingSettingsAsync(ShippingSettingsDto settings, CancellationToken cancellationToken = default);
    
    // Tax Settings
    Task<TaxSettingsDto> GetTaxSettingsAsync(CancellationToken cancellationToken = default);
    Task<TaxSettingsDto> UpdateTaxSettingsAsync(TaxSettingsDto settings, CancellationToken cancellationToken = default);
    
    // Notification Settings
    Task<NotificationSettingsDto> GetNotificationSettingsAsync(CancellationToken cancellationToken = default);
    Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(NotificationSettingsDto settings, CancellationToken cancellationToken = default);
    
    // Email Templates
    Task<IReadOnlyList<EmailTemplateDto>> GetEmailTemplatesAsync(CancellationToken cancellationToken = default);
    Task<EmailTemplateDto?> GetEmailTemplateAsync(string templateId, CancellationToken cancellationToken = default);
    Task<EmailTemplateDto> UpdateEmailTemplateAsync(string templateId, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default);
    Task<string> PreviewEmailTemplateAsync(string templateId, IDictionary<string, string>? sampleData = null, CancellationToken cancellationToken = default);
    Task SendTestEmailAsync(string templateId, string recipientEmail, CancellationToken cancellationToken = default);
    
    // Audit Logs
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 50,
        string? action = null,
        string? entityType = null,
        string? performedBy = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportAuditLogsAsync(
        string format,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    // Cache Management
    Task ClearCacheAsync(string? cacheKey = null, CancellationToken cancellationToken = default);
    Task<IDictionary<string, object>> GetCacheStatisticsAsync(CancellationToken cancellationToken = default);
}
