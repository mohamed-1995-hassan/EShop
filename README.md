# DDD E-Shop Demo

A comprehensive Domain-Driven Design demo with .NET 8, showcasing domain events, handlers, and Clean Architecture.

## 🎯 Features Demonstrated

### DDD Building Blocks
- **Entities** - Objects with identity (Order, OrderLine)
- **Value Objects** - Immutable objects (Money, Address)
- **Aggregate Root** - Entry point with invariants (Order)
- **Domain Events** - Things that happened (OrderPlaced, OrderShipped, OrderCancelled)
- **Repository** - Interface in Domain, implementation in Infrastructure

### Domain Events & Handlers
| Event | Handlers |
|-------|----------|
| `OrderPlacedEvent` | SendOrderConfirmationHandler, ReserveInventoryHandler, UpdateOrderAnalyticsHandler |
| `OrderShippedEvent` | SendShippingNotificationHandler |
| `OrderCancelledEvent` | ReleaseInventoryHandler, ProcessRefundHandler, SendCancellationEmailHandler |

### Clean Architecture Layers
```
┌─────────────────────────────────────────────────────────┐
│                    API Layer                            │
│              (Controllers, Swagger)                     │
├─────────────────────────────────────────────────────────┤
│                Application Layer                        │
│      (Services, DTOs, Event Handlers)                   │
├─────────────────────────────────────────────────────────┤
│               Infrastructure Layer                       │
│    (Repositories, External Services, Dispatchers)       │
├─────────────────────────────────────────────────────────┤
│                  Domain Layer                           │
│  (Entities, Value Objects, Aggregates, Events, Repos)   │
│              *** NO DEPENDENCIES ***                    │
└─────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK

### Run the Demo
```bash
cd DDD-Presentation/Demo
dotnet restore
dotnet run --project src/EShop.API
```

### Open Swagger UI
Navigate to: https://localhost:5001/swagger

## 📋 Demo Flow

### Option 1: Quick Demo (Recommended)
Call `POST /api/orders/demo` - Creates a complete order in one call and shows all events.

### Option 2: Step-by-Step
1. **Create Order**: `POST /api/orders`
```json
{
  "customerId": "11111111-1111-1111-1111-111111111111",
  "street": "123 Main St",
  "city": "Kuwait City",
  "country": "Kuwait",
  "postalCode": "12345"
}
```

2. **Add Items**: `POST /api/orders/{orderId}/items`
```json
{
  "productId": "22222222-2222-2222-2222-222222222222",
  "productName": "MacBook Pro",
  "unitPrice": 1299.99,
  "quantity": 1
}
```

3. **Place Order**: `POST /api/orders/{orderId}/place`
   - Watch the console for domain events!
   - Email confirmation handler
   - Inventory reservation handler
   - Analytics handler

4. **Ship Order**: `POST /api/orders/{orderId}/ship`
```json
{
  "trackingNumber": "TRK123456",
  "carrier": "FedEx"
}
```

5. **Cancel Order** (alternative): `POST /api/orders/{orderId}/cancel`
```json
{
  "reason": "Customer changed mind"
}
```

## 🔍 What to Watch

When you place an order, watch the console output:
```
═══════════════════════════════════════════════════
🛒 PLACING ORDER {OrderId}
═══════════════════════════════════════════════════
📢 Dispatching 1 domain event(s)
  → Publishing OrderPlacedEvent
📧 Sending order confirmation email...
📦 Reserving inventory...
📊 Recording analytics...
✅ ORDER {OrderId} PLACED SUCCESSFULLY
═══════════════════════════════════════════════════
```

## 📁 Project Structure

```
src/
├── EShop.Domain/           # Core domain (no dependencies)
│   ├── Aggregates/         # Order aggregate
│   ├── Common/             # Entity, AggregateRoot, DomainEvent
│   ├── Events/             # Domain events
│   ├── Exceptions/         # Domain exceptions
│   ├── Repositories/       # Repository interfaces
│   └── ValueObjects/       # Money, Address
│
├── EShop.Application/      # Application services
│   ├── DTOs/               # Request/Response DTOs
│   ├── EventHandlers/      # Domain event handlers
│   ├── Interfaces/         # Service interfaces
│   └── Services/           # Application services
│
├── EShop.Infrastructure/   # External concerns
│   ├── DomainEvents/       # Event dispatcher
│   ├── Repositories/       # Repository implementations
│   └── Services/           # Fake email, inventory, payment
│
└── EShop.API/              # Entry point
    └── Controllers/        # API controllers
```

## 🎓 Key Concepts Explained

### Why Domain Events?
```csharp
// Without events - tight coupling
public void Place() {
    _emailService.SendConfirmation();  // Domain knows about email!
    _inventoryService.Reserve();        // Domain knows about inventory!
}

// With events - loose coupling
public void Place() {
    AddDomainEvent(new OrderPlacedEvent(...));  // Domain just announces
    // Handlers in Application layer decide what to do
}
```

### Why Immutable Value Objects?
```csharp
// Mutable - DANGEROUS
var price = new MutableMoney(100);
var total = price;
total.Amount = 200;  // Oops! price is also 200 now!

// Immutable - SAFE
var price = new Money(100, "KWD");
var total = price.Add(new Money(50, "KWD"));  // New object created
// price is still 100, total is 150
```

### Why Aggregate Root?
```csharp
// External code can ONLY talk to Order (the root)
order.AddItem(productId, name, price, qty);  // ✅ OK

// NOT directly to OrderLine
var line = new OrderLine(...);  // ❌ Wrong!
order.OrderLines.Add(line);     // ❌ Can't! It's read-only
```

## 📚 Learn More

- [Domain-Driven Design by Eric Evans](https://www.amazon.com/Domain-Driven-Design-Tackling-Complexity-Software/dp/0321125215)
- [Clean Architecture by Robert Martin](https://www.amazon.com/Clean-Architecture-Craftsmans-Software-Structure/dp/0134494164)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
