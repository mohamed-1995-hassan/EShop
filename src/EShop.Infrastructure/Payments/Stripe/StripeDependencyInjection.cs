using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Infrastructure.Payments.Stripe;

/// <summary>
/// Dependency injection for Stripe services
/// </summary>
public static class StripeDependencyInjection
{
    /// <summary>
    /// Add Stripe payment services
    /// </summary>
    public static IServiceCollection AddStripePayments(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));

        // Services
        services.AddScoped<IStripePaymentService, StripePaymentService>();
        services.AddScoped<IStripeWebhookHandler, StripeWebhookHandler>();

        return services;
    }

    /// <summary>
    /// Add Stripe services for testing (mock configuration)
    /// </summary>
    public static IServiceCollection AddStripePaymentsForTesting(this IServiceCollection services)
    {
        services.Configure<StripeSettings>(options =>
        {
            options.SecretKey = "sk_test_mock";
            options.PublishableKey = "pk_test_mock";
            options.WebhookSecret = "whsec_mock";
            options.DefaultCurrency = "usd";
        });

        services.AddScoped<IStripePaymentService, StripePaymentService>();
        services.AddScoped<IStripeWebhookHandler, StripeWebhookHandler>();

        return services;
    }
}
