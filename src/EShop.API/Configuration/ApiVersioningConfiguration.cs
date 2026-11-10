using Asp.Versioning;

namespace EShop.API.Configuration;

/// <summary>
/// API versioning configuration.
/// Supports URL path versioning (/api/v1/orders), header versioning, and query string versioning.
/// </summary>
public static class ApiVersioningConfiguration
{
    /// <summary>
    /// Add API versioning services
    /// </summary>
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Default version when not specified
            options.DefaultApiVersion = new ApiVersion(1, 0);
            
            // Assume default version when client doesn't specify
            options.AssumeDefaultVersionWhenUnspecified = true;
            
            // Report supported versions in response headers
            options.ReportApiVersions = true;
            
            // Read version from multiple sources
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),           // /api/v1/orders
                new HeaderApiVersionReader("X-Api-Version"), // X-Api-Version: 1.0
                new QueryStringApiVersionReader("api-version")); // ?api-version=1.0
        })
        .AddApiExplorer(options =>
        {
            // Version format: 'v'major[.minor][-status]
            // e.g., v1, v1.0, v2.0-beta
            options.GroupNameFormat = "'v'VVV";
            
            // Substitute version in URL templates
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}

/// <summary>
/// API version constants
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Version 1.0 - Initial release
    /// </summary>
    public const string V1 = "1.0";
    
    /// <summary>
    /// Version 2.0 - Major update (future)
    /// </summary>
    public const string V2 = "2.0";
}
