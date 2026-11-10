using System.Text;
using EShop.Domain.Repositories;
using EShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EShop.Infrastructure.Identity;

/// <summary>
/// Extension methods for configuring Identity/Authentication services
/// </summary>
public static class IdentityDependencyInjection
{
    /// <summary>
    /// Add authentication and authorization services
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<PasswordSettings>(configuration.GetSection(PasswordSettings.SectionName));
        services.Configure<OAuthSettings>(configuration.GetSection(OAuthSettings.SectionName));

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Current user service
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // OAuth validation
        services.AddHttpClient("OAuth");
        services.AddScoped<IOAuthValidationService, OAuthValidationService>();

        // JWT Authentication
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = true; // Set to false only in development
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds)
            };

            // Handle WebSocket authentication (SignalR)
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["X-Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole(Domain.Aggregates.IdentityAggregate.AppRoles.Admin));
            
            options.AddPolicy("VendorOrAdmin", policy => 
                policy.RequireRole(
                    Domain.Aggregates.IdentityAggregate.AppRoles.Admin,
                    Domain.Aggregates.IdentityAggregate.AppRoles.Vendor));
            
            options.AddPolicy("CustomerOrAbove", policy => 
                policy.RequireRole(
                    Domain.Aggregates.IdentityAggregate.AppRoles.Admin,
                    Domain.Aggregates.IdentityAggregate.AppRoles.Vendor,
                    Domain.Aggregates.IdentityAggregate.AppRoles.Customer));
            
            options.AddPolicy("SupportOrAdmin", policy => 
                policy.RequireRole(
                    Domain.Aggregates.IdentityAggregate.AppRoles.Admin,
                    Domain.Aggregates.IdentityAggregate.AppRoles.Support));

            options.AddPolicy("EmailVerified", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "email_verified" && c.Value == "true")));
        });

        return services;
    }

    /// <summary>
    /// Add identity services for testing (in-memory)
    /// </summary>
    public static IServiceCollection AddIdentityServicesForTesting(this IServiceCollection services)
    {
        // Use in-memory settings
        services.Configure<JwtSettings>(options =>
        {
            options.Secret = "TestSecretKeyForDevelopmentOnly12345678901234567890";
            options.Issuer = "EShop.API.Test";
            options.Audience = "EShop.Test";
            options.AccessTokenExpirationMinutes = 15;
            options.RefreshTokenExpirationDays = 7;
        });

        services.Configure<PasswordSettings>(options =>
        {
            options.MinimumLength = 6;
            options.RequireUppercase = false;
            options.RequireLowercase = false;
            options.RequireDigit = false;
            options.RequireSpecialCharacter = false;
        });

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        return services;
    }
}
