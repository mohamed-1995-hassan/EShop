using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Middleware;

/// <summary>
/// GLOBAL EXCEPTION HANDLING MIDDLEWARE
/// 
/// Catches all unhandled exceptions and returns:
/// - RFC 7807 Problem Details format
/// - Appropriate HTTP status codes
/// - Correlation ID for tracking
/// - NO stack traces in production
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly bool _includeDetails;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        bool includeDetails = false)
    {
        _next = next;
        _logger = logger;
        _includeDetails = includeDetails;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}",
            correlationId, context.Request.Path);

        var (statusCode, problemDetails) = MapExceptionToProblemDetails(exception, correlationId);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private (int StatusCode, ProblemDetails Details) MapExceptionToProblemDetails(
        Exception exception, 
        string correlationId)
    {
        return exception switch
        {
            // Domain exceptions
            EShop.Domain.Exceptions.DomainException domainEx => (
                (int)HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    "Domain Rule Violation",
                    domainEx.Message,
                    HttpStatusCode.BadRequest,
                    correlationId,
                    exception)),

            // Validation exceptions
            FluentValidation.ValidationException validationEx => (
                (int)HttpStatusCode.BadRequest,
                CreateValidationProblemDetails(validationEx, correlationId)),

            // Not found
            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                CreateProblemDetails(
                    "Resource Not Found",
                    exception.Message,
                    HttpStatusCode.NotFound,
                    correlationId,
                    exception)),

            // Concurrency
            EventSourcing.ConcurrencyException concurrencyEx => (
                (int)HttpStatusCode.Conflict,
                CreateProblemDetails(
                    "Concurrency Conflict",
                    concurrencyEx.Message,
                    HttpStatusCode.Conflict,
                    correlationId,
                    exception)),

            // Rate limit
            RateLimitExceededException rateLimitEx => (
                429,
                CreateProblemDetails(
                    "Rate Limit Exceeded",
                    $"Too many requests. Retry after {rateLimitEx.RetryAfter.TotalSeconds} seconds.",
                    (HttpStatusCode)429,
                    correlationId,
                    exception,
                    new Dictionary<string, object>
                    {
                        ["retryAfter"] = rateLimitEx.RetryAfter.TotalSeconds
                    })),

            // Distributed lock
            DistributedLocking.DistributedLockException lockEx => (
                (int)HttpStatusCode.Conflict,
                CreateProblemDetails(
                    "Resource Locked",
                    lockEx.Message,
                    HttpStatusCode.Conflict,
                    correlationId,
                    exception)),

            // Unauthorized
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                CreateProblemDetails(
                    "Unauthorized",
                    "Authentication required",
                    HttpStatusCode.Unauthorized,
                    correlationId,
                    exception)),

            // Operation cancelled
            OperationCanceledException => (
                499, // Client Closed Request
                CreateProblemDetails(
                    "Request Cancelled",
                    "The request was cancelled",
                    (HttpStatusCode)499,
                    correlationId,
                    exception)),

            // Default - Internal Server Error
            _ => (
                (int)HttpStatusCode.InternalServerError,
                CreateProblemDetails(
                    "Internal Server Error",
                    _includeDetails ? exception.Message : "An unexpected error occurred",
                    HttpStatusCode.InternalServerError,
                    correlationId,
                    exception))
        };
    }

    private ProblemDetails CreateProblemDetails(
        string title,
        string detail,
        HttpStatusCode statusCode,
        string correlationId,
        Exception exception,
        Dictionary<string, object>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = (int)statusCode,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        if (_includeDetails)
        {
            problemDetails.Extensions["exception"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        if (extensions != null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        return problemDetails;
    }

    private ProblemDetails CreateValidationProblemDetails(
        FluentValidation.ValidationException exception,
        string correlationId)
    {
        var problemDetails = new ProblemDetails
        {
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred",
            Status = (int)HttpStatusCode.BadRequest,
            Type = "https://httpstatuses.com/400"
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["errors"] = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return problemDetails;
    }
}

/// <summary>
/// Custom exception for rate limit exceeded
/// </summary>
public class RateLimitExceededException : Exception
{
    public TimeSpan RetryAfter { get; }

    public RateLimitExceededException(TimeSpan retryAfter)
        : base($"Rate limit exceeded. Retry after {retryAfter.TotalSeconds} seconds.")
    {
        RetryAfter = retryAfter;
    }
}
