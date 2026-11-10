using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace EShop.API.Configuration;

/// <summary>
/// Response compression configuration for better performance.
/// Supports Brotli and Gzip compression.
/// </summary>
public static class CompressionConfiguration
{
    /// <summary>
    /// Add response compression services
    /// </summary>
    public static IServiceCollection AddCompressionConfiguration(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            // Enable compression for HTTPS (be aware of CRIME/BREACH attacks for sensitive data)
            options.EnableForHttps = true;

            // Compression providers (Brotli preferred, Gzip as fallback)
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            // MIME types to compress
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                // JSON (API responses)
                "application/json",
                "application/problem+json",
                
                // Text formats
                "text/plain",
                "text/csv",
                "text/xml",
                
                // JavaScript/CSS (if serving static files)
                "application/javascript",
                "text/css",
                
                // Images that can benefit from compression
                "image/svg+xml",
                
                // OpenAPI/Swagger
                "application/json; charset=utf-8"
            });

            // Exclude certain paths from compression
            options.ExcludedMimeTypes = new[]
            {
                // Already compressed
                "image/png",
                "image/jpeg",
                "image/gif",
                "image/webp",
                "application/zip",
                "application/pdf",
                
                // Streaming
                "text/event-stream"
            };
        });

        // Configure Brotli compression level
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            // Level 4 provides a good balance between compression ratio and speed
            // Range: 0 (no compression) to 11 (max compression)
            options.Level = CompressionLevel.Fastest; // Use Fastest for API responses
        });

        // Configure Gzip compression level
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }
}
