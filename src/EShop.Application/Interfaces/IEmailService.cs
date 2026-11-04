namespace EShop.Application.EventHandlers;

/// <summary>
/// Email service interface - defined in Application, implemented in Infrastructure.
/// Demonstrates: Dependency Inversion Principle
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        CancellationToken cancellationToken = default);
}
