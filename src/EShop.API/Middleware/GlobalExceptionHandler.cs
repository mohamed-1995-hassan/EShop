using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EShop.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Converts exceptions to RFC 7807 Problem Details format.
/// Hides sensitive error details in production.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        // Create problem details based on exception type
        var problemDetails = CreateProblemDetails(httpContext, exception);

        // Set response
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var (statusCode, title, type) = exception switch
        {
            // Domain exceptions (business rule violations)
            EShop.Domain.Exceptions.DomainException domainEx => 
                (StatusCodes.Status400BadRequest, "Business Rule Violation", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            // Validation exceptions
            FluentValidation.ValidationException validationEx =>
                (StatusCodes.Status400BadRequest, "Validation Failed", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            // Not found
            KeyNotFoundException or FileNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource Not Found", "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

            // Unauthorized
            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", "https://tools.ietf.org/html/rfc7235#section-3.1"),

            // Conflict (concurrency)
            InvalidOperationException when exception.Message.Contains("concurrency") =>
                (StatusCodes.Status409Conflict, "Conflict", "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            // Invalid operation
            InvalidOperationException =>
                (StatusCodes.Status400BadRequest, "Invalid Operation", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            // Argument exceptions
            ArgumentException or ArgumentNullException =>
                (StatusCodes.Status400BadRequest, "Invalid Argument", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            // Timeout
            TimeoutException or TaskCanceledException =>
                (StatusCodes.Status504GatewayTimeout, "Request Timeout", "https://tools.ietf.org/html/rfc7231#section-6.6.5"),

            // Rate limiting
            Exception ex when ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) =>
                (StatusCodes.Status429TooManyRequests, "Too Many Requests", "https://tools.ietf.org/html/rfc6585#section-4"),

            // Default: Internal server error
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Instance = context.Request.Path,
            Detail = GetDetailMessage(exception, statusCode)
        };

        // Add trace ID for correlation
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // Add validation errors for validation exceptions
        if (exception is FluentValidation.ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            
            problemDetails.Extensions["errors"] = errors;
        }

        // Add exception details in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace?.Split(Environment.NewLine).Take(10).ToArray(),
                innerException = exception.InnerException?.Message
            };
        }

        return problemDetails;
    }

    private string GetDetailMessage(Exception exception, int statusCode)
    {
        // In production, hide internal error details
        if (!_environment.IsDevelopment() && statusCode >= 500)
        {
            return "An unexpected error occurred. Please try again later.";
        }

        return exception switch
        {
            EShop.Domain.Exceptions.DomainException domainEx => domainEx.Message,
            FluentValidation.ValidationException => "One or more validation errors occurred.",
            KeyNotFoundException => "The requested resource was not found.",
            UnauthorizedAccessException => "You are not authorized to perform this action.",
            _ => exception.Message
        };
    }
}

/// <summary>
/// Exception handler middleware extensions
/// </summary>
public static class ExceptionHandlerExtensions
{
    /// <summary>
    /// Add global exception handling services
    /// </summary>
    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    /// <summary>
    /// Use global exception handling middleware
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        return app;
    }
}
