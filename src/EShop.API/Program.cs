using EShop.Application.EventHandlers;
using EShop.Application.Services;
using EShop.Domain.Repositories;
using EShop.Infrastructure.DomainEvents;
using EShop.Infrastructure.Repositories;
using EShop.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
// DEPENDENCY INJECTION - Following DDD + Clean Architecture principles
// ═══════════════════════════════════════════════════════════════════════════

// MediatR for domain events (scans assemblies for handlers)
builder.Services.AddMediatR(cfg => 
{
    // Register handlers from Application layer
    cfg.RegisterServicesFromAssembly(typeof(SendOrderConfirmationHandler).Assembly);
});

// ───────────────────────────────────────────────────────────────────────────
// DOMAIN LAYER - No dependencies to register (pure C# classes)
// ───────────────────────────────────────────────────────────────────────────

// ───────────────────────────────────────────────────────────────────────────
// APPLICATION LAYER - Application Services
// ───────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderAppService, OrderAppService>();

// ───────────────────────────────────────────────────────────────────────────
// INFRASTRUCTURE LAYER - Implementations of interfaces defined elsewhere
// ───────────────────────────────────────────────────────────────────────────

// Repository: Interface in Domain, Implementation in Infrastructure
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

// Domain Event Dispatcher
builder.Services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

// External Services (fakes for demo)
builder.Services.AddSingleton<IEmailService, FakeEmailService>();
builder.Services.AddSingleton<IInventoryService, FakeInventoryService>();
builder.Services.AddSingleton<IPaymentService, FakePaymentService>();

// ───────────────────────────────────────────────────────────────────────────
// ASP.NET Core / Presentation Layer
// ───────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "EShop DDD Demo API",
        Version = "v1",
        Description = """
            A complete Domain-Driven Design demo with:
            - Rich Domain Model (Order aggregate)
            - Domain Events (OrderPlaced, OrderShipped, OrderCancelled)
            - Event Handlers (Email, Inventory, Payment, Analytics)
            - Clean Architecture layers
            
            Try the endpoints in order:
            1. POST /api/orders/demo - Creates a complete demo order
            2. Or manually: Create → Add Items → Place → Ship
            
            Watch the console for domain events being handled!
            """
    });
});

// ═══════════════════════════════════════════════════════════════════════════
// CONFIGURE PIPELINE
// ═══════════════════════════════════════════════════════════════════════════

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EShop DDD Demo v1");
        c.DocumentTitle = "DDD Demo API";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Startup message
Console.WriteLine("""
    
    ╔═══════════════════════════════════════════════════════════════════════════╗
    ║                                                                           ║
    ║     🎯 DOMAIN-DRIVEN DESIGN DEMO - E-Shop API                             ║
    ║                                                                           ║
    ╠═══════════════════════════════════════════════════════════════════════════╣
    ║                                                                           ║
    ║  FEATURES DEMONSTRATED:                                                   ║
    ║  ─────────────────────                                                    ║
    ║  ✓ Rich Domain Model (Order aggregate with business rules)                ║
    ║  ✓ Value Objects (Money, Address - immutable)                             ║
    ║  ✓ Domain Events (OrderPlaced, OrderShipped, OrderCancelled)              ║
    ║  ✓ Event Handlers (Email, Inventory, Payment, Analytics)                  ║
    ║  ✓ Repository Pattern (interface in Domain, impl in Infrastructure)       ║
    ║  ✓ Clean Architecture (Domain → Application → Infrastructure → API)       ║
    ║                                                                           ║
    ║  HOW TO TEST:                                                             ║
    ║  ─────────────                                                            ║
    ║  1. Open Swagger UI at: https://localhost:5001/swagger                    ║
    ║  2. Call POST /api/orders/demo to see the full flow                       ║
    ║  3. Watch THIS CONSOLE for domain events being handled!                   ║
    ║                                                                           ║
    ║  EVENT FLOW:                                                              ║
    ║  ────────────                                                             ║
    ║  Order.Place() → raises OrderPlacedEvent                                  ║
    ║      ├─→ SendOrderConfirmationHandler (sends email)                       ║
    ║      ├─→ ReserveInventoryHandler (reserves stock)                         ║
    ║      └─→ UpdateOrderAnalyticsHandler (tracks metrics)                     ║
    ║                                                                           ║
    ╚═══════════════════════════════════════════════════════════════════════════╝
    
    """);

app.Run();
