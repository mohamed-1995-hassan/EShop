using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Configuration;

/// <summary>
/// CONFIGURATION VALIDATION
/// 
/// Validates all configuration on application startup.
/// Fails fast if required settings are missing or invalid.
/// </summary>
public static class ConfigurationValidation
{
    public static IServiceCollection AddConfigurationValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection("ConnectionStrings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // RabbitMQ
        services.AddOptions<RabbitMqValidationSettings>()
            .Bind(configuration.GetSection("RabbitMQ"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Redis
        services.AddOptions<RedisValidationSettings>()
            .Bind(configuration.GetSection("Redis"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Encryption (if enabled)
        if (configuration.GetValue<bool>("Encryption:Enabled", true))
        {
            services.AddOptions<EncryptionValidationSettings>()
                .Bind(configuration.GetSection("Encryption"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        return services;
    }
}

#region Validation Settings Classes

public class DatabaseSettings
{
    [Required(ErrorMessage = "Database connection string is required")]
    public string Database { get; set; } = string.Empty;
}

public class RabbitMqValidationSettings
{
    [Required(ErrorMessage = "RabbitMQ host is required")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "RabbitMQ port must be between 1 and 65535")]
    public int Port { get; set; } = 5672;

    [Required(ErrorMessage = "RabbitMQ username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "RabbitMQ password is required")]
    public string Password { get; set; } = string.Empty;
}

public class RedisValidationSettings
{
    [Required(ErrorMessage = "Redis configuration is required")]
    public string Configuration { get; set; } = string.Empty;
}

public class EncryptionValidationSettings
{
    [Required(ErrorMessage = "Encryption master key is required")]
    [MinLength(32, ErrorMessage = "Master key must be at least 32 characters")]
    public string MasterKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Encryption salt is required")]
    [MinLength(16, ErrorMessage = "Salt must be at least 16 characters")]
    public string Salt { get; set; } = string.Empty;
}

#endregion

/// <summary>
/// Startup validation extension
/// </summary>
public static class StartupValidation
{
    /// <summary>
    /// Validate all critical services are reachable
    /// </summary>
    public static async Task ValidateServicesAsync(IServiceProvider services)
    {
        var errors = new List<string>();

        // Check database
        try
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<Persistence.EShopDbContext>();
            if (dbContext != null)
            {
                await dbContext.Database.CanConnectAsync();
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Database: {ex.Message}");
        }

        // Check Redis
        try
        {
            var redis = services.GetService<StackExchange.Redis.IConnectionMultiplexer>();
            if (redis != null && !redis.IsConnected)
            {
                errors.Add("Redis: Not connected");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Redis: {ex.Message}");
        }

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Startup validation failed:\n{string.Join("\n", errors)}");
        }
    }
}
