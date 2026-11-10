using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers.Admin;

/// <summary>
/// Admin System Settings
/// </summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[Tags("Admin Settings")]
public class AdminSettingsController : ControllerBase
{
    private readonly IAdminSettingsService _settingsService;
    private readonly ILogger<AdminSettingsController> _logger;

    public AdminSettingsController(
        IAdminSettingsService settingsService,
        ILogger<AdminSettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    #region General Settings

    /// <summary>
    /// Get general settings
    /// </summary>
    [HttpGet("general")]
    [ProducesResponseType(typeof(GeneralSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GeneralSettingsDto>> GetGeneralSettings(
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetGeneralSettingsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update general settings
    /// </summary>
    [HttpPut("general")]
    [ProducesResponseType(typeof(GeneralSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GeneralSettingsDto>> UpdateGeneralSettings(
        [FromBody] GeneralSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.UpdateGeneralSettingsAsync(settings, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Payment Settings

    /// <summary>
    /// Get payment settings
    /// </summary>
    [HttpGet("payment")]
    [ProducesResponseType(typeof(PaymentSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSettingsDto>> GetPaymentSettings(
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetPaymentSettingsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update payment settings
    /// </summary>
    [HttpPut("payment")]
    [ProducesResponseType(typeof(PaymentSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentSettingsDto>> UpdatePaymentSettings(
        [FromBody] PaymentSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.UpdatePaymentSettingsAsync(settings, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Shipping Settings

    /// <summary>
    /// Get shipping settings
    /// </summary>
    [HttpGet("shipping")]
    [ProducesResponseType(typeof(ShippingSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShippingSettingsDto>> GetShippingSettings(
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetShippingSettingsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update shipping settings
    /// </summary>
    [HttpPut("shipping")]
    [ProducesResponseType(typeof(ShippingSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShippingSettingsDto>> UpdateShippingSettings(
        [FromBody] ShippingSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.UpdateShippingSettingsAsync(settings, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Tax Settings

    /// <summary>
    /// Get tax settings
    /// </summary>
    [HttpGet("tax")]
    [ProducesResponseType(typeof(TaxSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaxSettingsDto>> GetTaxSettings(
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetTaxSettingsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update tax settings
    /// </summary>
    [HttpPut("tax")]
    [ProducesResponseType(typeof(TaxSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaxSettingsDto>> UpdateTaxSettings(
        [FromBody] TaxSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.UpdateTaxSettingsAsync(settings, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Notification Settings

    /// <summary>
    /// Get notification settings
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> GetNotificationSettings(
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetNotificationSettingsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update notification settings
    /// </summary>
    [HttpPut("notifications")]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> UpdateNotificationSettings(
        [FromBody] NotificationSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.UpdateNotificationSettingsAsync(settings, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Email Templates

    /// <summary>
    /// Get all email templates
    /// </summary>
    [HttpGet("email-templates")]
    [ProducesResponseType(typeof(IReadOnlyList<EmailTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmailTemplateDto>>> GetEmailTemplates(
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetEmailTemplatesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get email template by ID
    /// </summary>
    [HttpGet("email-templates/{templateId}")]
    [ProducesResponseType(typeof(EmailTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailTemplateDto>> GetEmailTemplate(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetEmailTemplateAsync(templateId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Update email template
    /// </summary>
    [HttpPut("email-templates/{templateId}")]
    [ProducesResponseType(typeof(EmailTemplateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailTemplateDto>> UpdateEmailTemplate(
        string templateId,
        [FromBody] UpdateEmailTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.UpdateEmailTemplateAsync(templateId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Preview email template
    /// </summary>
    [HttpPost("email-templates/{templateId}/preview")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> PreviewEmailTemplate(
        string templateId,
        [FromBody] IDictionary<string, string>? sampleData = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.PreviewEmailTemplateAsync(templateId, sampleData, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Send test email
    /// </summary>
    [HttpPost("email-templates/{templateId}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendTestEmail(
        string templateId,
        [FromBody] SendTestEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        await _settingsService.SendTestEmailAsync(templateId, request.RecipientEmail, cancellationToken);
        return Ok(new { message = "Test email sent" });
    }

    #endregion

    #region Audit Logs

    /// <summary>
    /// Get audit logs
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? performedBy = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetAuditLogsAsync(
            page, pageSize, action, entityType, performedBy,
            startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Export audit logs
    /// </summary>
    [HttpGet("audit-logs/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _settingsService.ExportAuditLogsAsync(format, startDate, endDate, cancellationToken);
        var contentType = format.ToLowerInvariant() == "csv" ? "text/csv" : "application/json";
        var extension = format.ToLowerInvariant() == "csv" ? "csv" : "json";
        return File(data, contentType, $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}");
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Clear cache
    /// </summary>
    [HttpPost("cache/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCache(
        [FromBody] ClearCacheRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        await _settingsService.ClearCacheAsync(request?.CacheKey, cancellationToken);
        return Ok(new { message = request?.CacheKey != null ? $"Cache key '{request.CacheKey}' cleared" : "All cache cleared" });
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("cache/statistics")]
    [ProducesResponseType(typeof(IDictionary<string, object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IDictionary<string, object>>> GetCacheStatistics(
        CancellationToken cancellationToken = default)
    {
        var result = await _settingsService.GetCacheStatisticsAsync(cancellationToken);
        return Ok(result);
    }

    #endregion
}

#region Request DTOs

public class SendTestEmailRequest
{
    public string RecipientEmail { get; init; } = string.Empty;
}

public class ClearCacheRequest
{
    public string? CacheKey { get; init; }
}

#endregion
