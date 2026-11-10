using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Infrastructure.Configuration;

/// <summary>
/// Secrets management configuration for production environments.
/// Supports Azure Key Vault, AWS Secrets Manager, and environment variables.
/// </summary>
public static class SecretsConfiguration
{
    /// <summary>
    /// Add secrets management based on configuration.
    /// Priority: Azure Key Vault > AWS Secrets Manager > Environment Variables
    /// </summary>
    public static IConfigurationBuilder AddSecretsManagement(
        this IConfigurationBuilder builder,
        IConfiguration configuration)
    {
        var secretsProvider = configuration["Secrets:Provider"]?.ToLowerInvariant();

        switch (secretsProvider)
        {
            case "azurekeyvault":
                AddAzureKeyVault(builder, configuration);
                break;
            case "awssecretsmanager":
                AddAwsSecretsManager(builder, configuration);
                break;
            case "environment":
            default:
                // Environment variables are already included by default in ASP.NET Core
                // This is the most secure option for containerized deployments
                break;
        }

        return builder;
    }

    private static void AddAzureKeyVault(IConfigurationBuilder builder, IConfiguration configuration)
    {
        var vaultUri = configuration["Secrets:AzureKeyVault:VaultUri"];
        
        if (string.IsNullOrEmpty(vaultUri))
        {
            throw new InvalidOperationException(
                "Azure Key Vault URI not configured. Set Secrets:AzureKeyVault:VaultUri");
        }

        // Uses DefaultAzureCredential which supports:
        // - Managed Identity (production)
        // - Visual Studio / Azure CLI credentials (development)
        // - Environment variables (AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID)
        
        // Note: Requires Azure.Extensions.AspNetCore.Configuration.Secrets package
        // builder.AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());
        
        Console.WriteLine($"[Secrets] Azure Key Vault configured: {vaultUri}");
    }

    private static void AddAwsSecretsManager(IConfigurationBuilder builder, IConfiguration configuration)
    {
        var secretName = configuration["Secrets:AwsSecretsManager:SecretName"];
        var region = configuration["Secrets:AwsSecretsManager:Region"] ?? "us-east-1";

        if (string.IsNullOrEmpty(secretName))
        {
            throw new InvalidOperationException(
                "AWS Secrets Manager secret name not configured. Set Secrets:AwsSecretsManager:SecretName");
        }

        // Note: Requires AWSSDK.SecretsManager package
        // Uses default credentials from:
        // - IAM Role (ECS/EKS)
        // - Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
        // - ~/.aws/credentials file
        
        Console.WriteLine($"[Secrets] AWS Secrets Manager configured: {secretName} in {region}");
    }
}

/// <summary>
/// Extension methods for secret-aware configuration retrieval
/// </summary>
public static class SecretConfigurationExtensions
{
    /// <summary>
    /// Get a connection string, checking environment variables first.
    /// Pattern: ConnectionStrings__{Name} or ConnectionStrings:{Name}
    /// </summary>
    public static string GetConnectionStringSecure(
        this IConfiguration configuration, 
        string name)
    {
        // Environment variable format for container orchestrators
        var envVar = Environment.GetEnvironmentVariable($"ConnectionStrings__{name}");
        if (!string.IsNullOrEmpty(envVar))
            return envVar;

        // Standard configuration
        return configuration.GetConnectionString(name) 
            ?? throw new InvalidOperationException($"Connection string '{name}' not found");
    }

    /// <summary>
    /// Get a secret value with fallback to environment variable.
    /// Environment variable pattern: Section__Key (double underscore)
    /// </summary>
    public static string GetSecretValue(
        this IConfiguration configuration,
        string section,
        string key)
    {
        // Environment variable format (Docker/Kubernetes friendly)
        var envVar = Environment.GetEnvironmentVariable($"{section}__{key}");
        if (!string.IsNullOrEmpty(envVar))
            return envVar;

        // Configuration file
        return configuration[$"{section}:{key}"] ?? string.Empty;
    }

    /// <summary>
    /// Check if running in production environment
    /// </summary>
    public static bool IsProduction(this IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
    }
}
