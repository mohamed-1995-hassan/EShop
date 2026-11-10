using Microsoft.OpenApi.Models;
using System.Reflection;

namespace EShop.API.Extensions;

/// <summary>
/// Swagger/OpenAPI configuration extensions for comprehensive API documentation
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Add enhanced Swagger documentation with security, XML comments, and contact info
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EShop DDD API",
                Version = "v1",
                Description = GetApiDescription(),
                Contact = new OpenApiContact
                {
                    Name = "EShop Team",
                    Email = "api@eshop.com",
                    Url = new Uri("https://github.com/eshop/api")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                },
                TermsOfService = new Uri("https://eshop.com/terms")
            });

            // JWT Bearer Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = """
                    JWT Authorization header using the Bearer scheme.
                    
                    Enter your token in the text input below.
                    
                    Example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                    """
            });

            // API Key Authentication (for webhooks)
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "API Key for webhook authentication"
            });

            // Apply security requirement globally
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments if available
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Custom operation filters
            options.EnableAnnotations();
            
            // Group by controller/tag
            options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
            options.DocInclusionPredicate((name, api) => true);

            // Add response types
            options.OperationFilter<SwaggerResponseOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Configure Swagger UI with enhanced features
    /// </summary>
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "api-docs/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/api-docs/v1/swagger.json", "EShop API v1");
            options.RoutePrefix = "swagger";
            
            // UI Customization
            options.DocumentTitle = "EShop API Documentation";
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableTryItOutByDefault();
            
            // Syntax highlighting
            options.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
            {
                ["activated"] = true,
                ["theme"] = "monokai"
            };
        });

        // Also serve ReDoc for alternative documentation view
        app.UseReDoc(options =>
        {
            options.RoutePrefix = "docs";
            options.SpecUrl = "/api-docs/v1/swagger.json";
            options.DocumentTitle = "EShop API Reference";
            options.EnableUntrustedSpec();
            options.ScrollYOffset(10);
            options.HideHostname();
            options.HideDownloadButton();
            options.ExpandResponses("200,201");
            options.RequiredPropsFirst();
            options.SortPropsAlphabetically();
        });

        return app;
    }

    private static string GetApiDescription()
    {
        return """
            # EShop API - Domain-Driven Design E-Commerce Platform

            A complete e-commerce API built with **Domain-Driven Design** principles, demonstrating:

            ## Architecture
            - **28 Aggregates** with rich domain models
            - **Clean Architecture** with clear layer separation
            - **CQRS Pattern** for command/query separation
            - **Domain Events** for decoupled integrations

            ## Features
            - 🔐 **Authentication** - JWT with refresh token rotation, OAuth (Google, Facebook, Apple, Microsoft)
            - 💳 **Payments** - Stripe integration with webhooks
            - 📧 **Email** - SendGrid, Amazon SES, SMTP support
            - 📦 **Orders** - Full lifecycle management with saga orchestration
            - 🛒 **Cart** - Shopping cart with promotions
            - 📊 **Analytics** - Sales and customer insights

            ## Getting Started

            ### Authentication
            1. Register: `POST /api/auth/register`
            2. Login: `POST /api/auth/login`
            3. Use the returned `accessToken` in the Authorization header

            ### API Conventions
            - All timestamps are in UTC (ISO 8601 format)
            - Money amounts include currency code (e.g., `{"amount": 99.99, "currency": "USD"}`)
            - Pagination: `page` (1-indexed), `pageSize` (default: 20, max: 100)
            - Error responses follow RFC 7807 Problem Details format

            ## Rate Limits
            | Endpoint | Limit |
            |----------|-------|
            | Authentication | 10 req/min |
            | Orders | 100 req/min |
            | Products | 1000 req/min |

            ## Environments
            | Environment | URL |
            |-------------|-----|
            | Production | https://api.eshop.com |
            | Staging | https://staging.api.eshop.com |
            | Development | http://localhost:5000 |
            """;
    }
}

/// <summary>
/// Operation filter to add common response types
/// </summary>
public class SwaggerResponseOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        // Add common error responses
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized - Invalid or missing authentication token" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden - Insufficient permissions" });
        operation.Responses.TryAdd("429", new OpenApiResponse { Description = "Too Many Requests - Rate limit exceeded" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Internal Server Error" });
    }
}
