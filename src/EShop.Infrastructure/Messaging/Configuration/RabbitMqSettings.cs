namespace EShop.Infrastructure.Messaging.Configuration;

/// <summary>
/// RabbitMQ connection settings.
/// Configure in appsettings.json under "RabbitMQ" section.
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// RabbitMQ host address
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port (default: 5672)
    /// </summary>
    public ushort Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host (default: "/")
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Prefetch count - number of messages to fetch at once
    /// </summary>
    public ushort PrefetchCount { get; set; } = 16;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable SSL for connection
    /// </summary>
    public bool UseSsl { get; set; } = false;
}

/// <summary>
/// Dead Letter Queue settings
/// </summary>
public class DlqSettings
{
    /// <summary>
    /// Maximum retry attempts before sending to DLQ
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Initial retry delay
    /// </summary>
    public int InitialRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum retry delay
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 60000;

    /// <summary>
    /// Retry delay multiplier for exponential backoff
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = 2.0;

    /// <summary>
    /// DLQ message TTL in hours (for automatic cleanup)
    /// </summary>
    public int MessageTtlHours { get; set; } = 168; // 7 days
}
