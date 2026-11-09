# EShop Infrastructure - Production-Ready Enterprise Patterns

## Overview

This infrastructure layer implements comprehensive enterprise patterns for production-grade applications:

### Core Messaging
- **Outbox Pattern** - Reliable event publishing with transactional consistency
- **Inbox Pattern** - Idempotent message processing (exactly-once semantics)
- **Saga Pattern** - Long-running distributed transaction coordination
- **Dead Letter Queue (DLQ)** - Failed message recovery and monitoring

### Scalability & Performance
- **Distributed Caching** - Redis-based caching for high performance
- **Rate Limiting** - Protect services from overload (Fixed/Sliding Window, Token Bucket)
- **Distributed Locking** - RedLock algorithm for distributed mutex

### Resilience
- **Polly Policies** - Retry, Circuit Breaker, Timeout
- **Retry Scheduler** - Scheduled retries with exponential backoff
- **Health Checks** - RabbitMQ, Outbox, DLQ monitoring

### Observability
- **OpenTelemetry** - Distributed tracing and metrics
- **Prometheus** - Metrics export for monitoring dashboards
- **Custom Metrics** - Orders, Payments, Messaging metrics

### Security
- **Message Encryption** - AES-256-GCM field-level encryption
- **Idempotency Keys** - API-level duplicate request protection

### Event Sourcing & CQRS (Optional)
- **Event Store** - PostgreSQL-based event storage
- **Projections** - Build optimized read models from events

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    API Layer                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐     │
│  │  Rate Limiter   │  │  Idempotency    │  │  Tracing        │  │  Metrics        │     │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘     │
├───────────┴────────────────────┴────────────────────┴────────────────────┴──────────────┤
│                                 Application Layer                                         │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐    │
│  │                              Domain Services                                      │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │    │
│  │  │ Order Svc   │  │ Payment Svc │  │ Inventory   │  │ Customer    │              │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘              │    │
│  └─────────────────────────────────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                               Infrastructure Layer                                        │
│                                                                                           │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐ │
│  │    PostgreSQL    │  │     RabbitMQ     │  │      Redis       │  │   OpenTelemetry  │ │
│  │  ┌────────────┐  │  │  ┌────────────┐  │  │  ┌────────────┐  │  │  ┌────────────┐  │ │
│  │  │   Orders   │  │  │  │  Exchanges │  │  │  │   Cache    │  │  │  │   Traces   │  │ │
│  │  │  Products  │  │  │  │   Queues   │  │  │  │   Locks    │  │  │  │   Metrics  │  │ │
│  │  │  Outbox    │  │  │  │   DLQ      │  │  │  │ Rate Limit │  │  │  │   Logs     │  │ │
│  │  │  Inbox     │  │  │  └────────────┘  │  │  │ Idempotency│  │  │  └────────────┘  │ │
│  │  │  Sagas     │  │  │                  │  │  └────────────┘  │  │                  │ │
│  │  │  Events    │  │  │                  │  │                  │  │                  │ │
│  │  └────────────┘  │  │                  │  │                  │  │                  │ │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  └──────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Patterns Detail

### 1. Outbox Pattern
Guarantees at-least-once event delivery with transactional consistency.

```csharp
// Events saved in same transaction as entity changes
await _dbContext.SaveChangesWithOutboxAsync();
```

### 2. Inbox Pattern
Achieves exactly-once processing via idempotency.

```csharp
await _inboxService.TryProcessAsync(
    message.EventId,
    nameof(OrderPlacedEvent),
    nameof(OrderPlacedConsumer),
    async () => await ProcessOrder(message));
```

### 3. Saga Pattern (Order Processing)

```
OrderPlaced → ReservingStock → ProcessingPayment → Confirmed
                   │                    │
                   ▼                    ▼
           StockReserveFailed     PaymentFailed
                   │                    │
                   ▼                    ▼
              Cancelled          ReleasingStock → Cancelled
```

### 4. Distributed Caching

```csharp
var product = await _cache.GetOrSetAsync(
    CacheKeys.Product(productId),
    () => _repository.GetByIdAsync(productId),
    CacheOptions.LongLived);
```

### 5. Rate Limiting

```csharp
var result = await _rateLimiter.CheckAsync(
    clientId,
    RateLimitPolicy.Payment);

if (!result.IsAllowed)
    return TooManyRequests(result.RetryAfter);
```

### 6. Distributed Locking

```csharp
await _lockService.ExecuteWithLockAsync(
    $"order:{orderId}",
    TimeSpan.FromSeconds(30),
    async () => await ProcessPayment(orderId));
```

### 7. API Idempotency

```csharp
var result = await _idempotency.ExecuteAsync(
    idempotencyKey,
    () => _orderService.CreateOrderAsync(request),
    TimeSpan.FromHours(24));

if (result.WasCached)
    Response.Headers["X-Idempotent-Replay"] = "true";
```

### 8. Message Encryption

```csharp
public class PaymentRequest
{
    public Guid OrderId { get; set; }
    
    [Encrypted]
    public string CardNumber { get; set; }
    
    [Encrypted]
    public string CVV { get; set; }
}

var encrypted = await _encryption.EncryptFieldsAsync(request);
```

### 9. Resilience with Polly

```csharp
await _resiliencePipeline.ExecuteAsync(async ct =>
{
    await _paymentGateway.ProcessAsync(payment, ct);
}, cancellationToken);
```

### 10. Event Sourcing (Optional)

```csharp
// Append events to stream
await _eventStore.AppendToStreamAsync(
    $"order-{orderId}",
    events,
    expectedVersion);

// Read order history
var events = await _eventStore.ReadStreamAsync($"order-{orderId}");
```

### 11. CQRS Projections (Optional)

```csharp
// Read model is automatically built from events
var orderView = await _orderReadModelRepository.GetByIdAsync(orderId);
var dashboard = await _orderReadModelRepository.GetDashboardAsync();
```

---

## Configuration

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Database=eshop;...",
    "Redis": "localhost:6379"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672
  },
  "Encryption": {
    "MasterKey": "from-key-vault",
    "Salt": "unique-salt"
  },
  "Observability": {
    "OtlpEndpoint": "http://localhost:4317"
  },
  "EventSourcing": { "Enabled": false },
  "CQRS": { "Enabled": false }
}
```

---

## Registration

```csharp
// Full production stack
services.AddInfrastructure(configuration);

// Without messaging (simpler)
services.AddInfrastructureWithoutMessaging(configuration);

// Minimal (database + repositories only)
services.AddMinimalInfrastructure(configuration);

// Testing (in-memory everything)
services.AddInfrastructureForTesting();
```

---

## Health Endpoints

```
GET /health             - Overall health
GET /health/ready       - Readiness probe (Kubernetes)
GET /health/live        - Liveness probe (Kubernetes)
GET /metrics            - Prometheus metrics
```

---

## Database Tables

| Table | Purpose |
|-------|---------|
| `orders` | Order aggregate |
| `products` | Product aggregate |
| `customers` | Customer entity |
| `outbox_messages` | Pending events |
| `inbox_messages` | Processed message IDs |
| `order_processing_sagas` | Saga state |
| `dead_letter_messages` | Failed messages |
| `event_store` | Event sourcing (optional) |

---

## Monitoring Metrics

**Orders:**
- `orders_created_total`
- `orders_completed_total`
- `order_processing_duration_seconds`

**Payments:**
- `payments_processed_total`
- `payments_failed_total`
- `payment_processing_duration_seconds`

**Messaging:**
- `messages_published_total`
- `messages_consumed_total`
- `messages_failed_total`
- `dead_letter_messages_total`
- `sagas_started_total`
- `sagas_completed_total`

---

## Best Practices Implemented

✅ ACID transactions for entity + outbox  
✅ Idempotent message processing  
✅ Saga compensating transactions  
✅ Exponential backoff with jitter  
✅ Circuit breaker for external services  
✅ Field-level encryption for PII  
✅ Distributed tracing correlation  
✅ Health checks for all dependencies  
✅ Graceful shutdown handling  
✅ Optimistic concurrency control  
