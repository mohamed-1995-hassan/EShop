using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EShop.Infrastructure.Messaging.DeadLetter;

/// <summary>
/// DEAD LETTER QUEUE (DLQ) PROCESSOR
/// 
/// Handles messages that failed processing after all retry attempts.
/// 
/// DLQ RECOVERY STRATEGIES:
/// 1. MANUAL INSPECTION: Store for manual review and fix
/// 2. AUTO-RETRY: Retry after longer delay (for transient issues)
/// 3. COMPENSATION: Execute compensating transaction
/// 4. ALERT: Notify operations team
/// 5. ARCHIVE: Move to cold storage for compliance
/// 
/// This processor provides visibility and recovery options for failed messages.
/// </summary>
public class DeadLetterProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadLetterProcessor> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public DeadLetterProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<DeadLetterProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dead Letter Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDeadLettersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dead letter messages");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessDeadLettersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetService<IDeadLetterRepository>();
        
        if (repository == null)
        {
            return;
        }

        var deadLetters = await repository.GetPendingAsync(50, cancellationToken);
        
        foreach (var deadLetter in deadLetters)
        {
            try
            {
                await ProcessDeadLetterAsync(deadLetter, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to process dead letter {Id} of type {Type}", 
                    deadLetter.Id, deadLetter.MessageType);
            }
        }
    }

    private async Task ProcessDeadLetterAsync(DeadLetterMessage deadLetter, CancellationToken cancellationToken)
    {
        // Determine recovery strategy based on message type and error
        var strategy = DetermineRecoveryStrategy(deadLetter);

        _logger.LogInformation(
            "Processing dead letter {Id} with strategy {Strategy}",
            deadLetter.Id, strategy);

        switch (strategy)
        {
            case RecoveryStrategy.AutoRetry:
                await RetryMessageAsync(deadLetter, cancellationToken);
                break;
                
            case RecoveryStrategy.Compensate:
                await ExecuteCompensationAsync(deadLetter, cancellationToken);
                break;
                
            case RecoveryStrategy.Alert:
                await SendAlertAsync(deadLetter, cancellationToken);
                break;
                
            case RecoveryStrategy.Archive:
                await ArchiveMessageAsync(deadLetter, cancellationToken);
                break;
                
            case RecoveryStrategy.ManualReview:
            default:
                // Leave for manual inspection
                _logger.LogWarning(
                    "Dead letter {Id} requires manual review. Type: {Type}, Error: {Error}",
                    deadLetter.Id, deadLetter.MessageType, deadLetter.ErrorMessage);
                break;
        }
    }

    private RecoveryStrategy DetermineRecoveryStrategy(DeadLetterMessage deadLetter)
    {
        // Transient errors - auto retry
        if (IsTransientError(deadLetter.ErrorMessage))
        {
            return deadLetter.RetryCount < 10 
                ? RecoveryStrategy.AutoRetry 
                : RecoveryStrategy.ManualReview;
        }

        // Business rule violations - compensate
        if (deadLetter.MessageType.Contains("Payment") || 
            deadLetter.MessageType.Contains("Stock"))
        {
            return RecoveryStrategy.Compensate;
        }

        // Old messages - archive
        if (deadLetter.FailedAtUtc < DateTime.UtcNow.AddDays(-7))
        {
            return RecoveryStrategy.Archive;
        }

        return RecoveryStrategy.ManualReview;
    }

    private static bool IsTransientError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage)) return false;
        
        var transientIndicators = new[]
        {
            "timeout",
            "connection",
            "unavailable",
            "temporarily",
            "retry",
            "network",
            "socket"
        };

        return transientIndicators.Any(i => 
            errorMessage.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    private Task RetryMessageAsync(DeadLetterMessage deadLetter, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduling retry for dead letter {Id}", deadLetter.Id);
        // Implementation: Re-publish to original queue
        return Task.CompletedTask;
    }

    private Task ExecuteCompensationAsync(DeadLetterMessage deadLetter, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing compensation for dead letter {Id}", deadLetter.Id);
        // Implementation: Execute compensating transaction based on message type
        return Task.CompletedTask;
    }

    private Task SendAlertAsync(DeadLetterMessage deadLetter, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "ALERT: Critical dead letter {Id} of type {Type}. Error: {Error}",
            deadLetter.Id, deadLetter.MessageType, deadLetter.ErrorMessage);
        // Implementation: Send to alerting system (PagerDuty, Slack, etc.)
        return Task.CompletedTask;
    }

    private Task ArchiveMessageAsync(DeadLetterMessage deadLetter, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Archiving old dead letter {Id}", deadLetter.Id);
        // Implementation: Move to cold storage (S3, archive database, etc.)
        return Task.CompletedTask;
    }
}

public enum RecoveryStrategy
{
    ManualReview,
    AutoRetry,
    Compensate,
    Alert,
    Archive
}
