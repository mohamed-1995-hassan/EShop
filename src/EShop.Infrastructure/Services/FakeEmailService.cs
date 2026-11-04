using EShop.Application.EventHandlers;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Services;

/// <summary>
/// Fake email service for demo purposes.
/// In real app: would use SendGrid, AWS SES, etc.
/// </summary>
public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;
    
    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
    }
    
    public Task SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            
            ═══════════════════════════════════════════════════
            📨 EMAIL SENT (Simulated)
            ───────────────────────────────────────────────────
            To:      {To}
            Subject: {Subject}
            ───────────────────────────────────────────────────
            {Body}
            ═══════════════════════════════════════════════════
            
            """,
            to, subject, body);
        
        return Task.CompletedTask;
    }
}
