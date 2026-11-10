using EShop.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Infrastructure.Email;

/// <summary>
/// Dependency injection extensions for email services
/// </summary>
public static class EmailDependencyInjection
{
    /// <summary>
    /// Add email services with automatic provider selection based on configuration
    /// </summary>
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<EmailConfiguration>(
            configuration.GetSection(EmailConfiguration.SectionName));

        // Get provider from configuration
        var emailConfig = configuration
            .GetSection(EmailConfiguration.SectionName)
            .Get<EmailConfiguration>() ?? new EmailConfiguration();

        // Register template service
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        // Register provider-specific service
        switch (emailConfig.Provider.ToLowerInvariant())
        {
            case "sendgrid":
                services.AddSendGridEmailService();
                break;

            case "amazonses":
            case "ses":
                services.AddAmazonSesEmailService();
                break;

            case "smtp":
                services.AddSmtpEmailService();
                break;

            case "fake":
            default:
                services.AddFakeEmailService();
                break;
        }

        return services;
    }

    /// <summary>
    /// Add SendGrid email service
    /// </summary>
    public static IServiceCollection AddSendGridEmailService(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, SendGridEmailService>();
        return services;
    }

    /// <summary>
    /// Add Amazon SES email service
    /// </summary>
    public static IServiceCollection AddAmazonSesEmailService(this IServiceCollection services)
    {
        services.AddHttpClient("AmazonSES", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IEmailService, AmazonSesEmailService>();
        return services;
    }

    /// <summary>
    /// Add SMTP email service
    /// </summary>
    public static IServiceCollection AddSmtpEmailService(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, SmtpEmailService>();
        return services;
    }

    /// <summary>
    /// Add fake email service (for development/testing)
    /// </summary>
    public static IServiceCollection AddFakeEmailService(this IServiceCollection services)
    {
        services.AddSingleton<IEmailService, FakeEmailService>();
        return services;
    }
}
