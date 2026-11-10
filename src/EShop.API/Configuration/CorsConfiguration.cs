namespace EShop.API.Configuration;

/// <summary>
/// CORS (Cross-Origin Resource Sharing) configuration.
/// Required for frontend applications to call the API.
/// </summary>
public static class CorsConfiguration
{
    public const string PolicyName = "EShopCorsPolicy";

    /// <summary>
    /// Add CORS services with configurable origins
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                // Configure allowed origins
                if (corsSettings.AllowedOrigins?.Length > 0)
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins);
                }
                else
                {
                    // Development default - allow localhost
                    policy.WithOrigins(
                        "http://localhost:3000",  // React default
                        "http://localhost:5173",  // Vite default
                        "http://localhost:4200",  // Angular default
                        "http://localhost:8080",  // Vue default
                        "https://localhost:3000",
                        "https://localhost:5173");
                }

                // Configure allowed methods
                if (corsSettings.AllowedMethods?.Length > 0)
                {
                    policy.WithMethods(corsSettings.AllowedMethods);
                }
                else
                {
                    policy.WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS");
                }

                // Configure allowed headers
                if (corsSettings.AllowedHeaders?.Length > 0)
                {
                    policy.WithHeaders(corsSettings.AllowedHeaders);
                }
                else
                {
                    policy.AllowAnyHeader();
                }

                // Allow credentials (cookies, authorization headers)
                if (corsSettings.AllowCredentials)
                {
                    policy.AllowCredentials();
                }

                // Expose headers for client access
                if (corsSettings.ExposedHeaders?.Length > 0)
                {
                    policy.WithExposedHeaders(corsSettings.ExposedHeaders);
                }
                else
                {
                    policy.WithExposedHeaders(
                        "X-Pagination",
                        "X-Total-Count",
                        "X-Request-Id",
                        "X-Correlation-Id",
                        "X-RateLimit-Limit",
                        "X-RateLimit-Remaining",
                        "X-RateLimit-Reset",
                        "Location",
                        "Content-Disposition");
                }

                // Set preflight cache duration
                policy.SetPreflightMaxAge(TimeSpan.FromMinutes(corsSettings.PreflightMaxAgeMinutes));
            });

            // Add a permissive policy for development
            options.AddPolicy("Development", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("*");
            });
        });

        return services;
    }
}

/// <summary>
/// CORS configuration settings
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// Allowed origins (e.g., "https://app.eshop.com")
    /// </summary>
    public string[]? AllowedOrigins { get; set; }

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public string[]? AllowedMethods { get; set; }

    /// <summary>
    /// Allowed headers
    /// </summary>
    public string[]? AllowedHeaders { get; set; }

    /// <summary>
    /// Headers to expose to the client
    /// </summary>
    public string[]? ExposedHeaders { get; set; }

    /// <summary>
    /// Allow credentials (cookies, auth headers)
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Preflight cache duration in minutes
    /// </summary>
    public int PreflightMaxAgeMinutes { get; set; } = 10;
}
