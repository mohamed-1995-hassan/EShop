using EShop.Application.Commands.Orders;
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

// MediatR for CQRS and domain events (scans Application assembly for all handlers)
builder.Services.AddMediatR(cfg => 
{
    // Register ALL handlers from Application layer (Commands, Queries, Events)
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly);
});

// ───────────────────────────────────────────────────────────────────────────
// DOMAIN LAYER - No dependencies to register (pure C# classes)
// ───────────────────────────────────────────────────────────────────────────

// ───────────────────────────────────────────────────────────────────────────
// APPLICATION LAYER - Application Services
// ───────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderAppService, OrderAppService>();

// ───────────────────────────────────────────────────────────────────────────
// INFRASTRUCTURE LAYER - Repository Implementations
// ───────────────────────────────────────────────────────────────────────────

// Repositories (Interface in Domain, Implementation in Infrastructure)
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<ICarrierRepository, CarrierRepository>();
builder.Services.AddScoped<IReturnRepository, ReturnRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IStockTransferRepository, StockTransferRepository>();

// New Aggregate Repositories
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IGiftCardRepository, GiftCardRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<ITaxConfigurationRepository, TaxConfigurationRepository>();
builder.Services.AddScoped<ICurrencyConfigurationRepository, CurrencyConfigurationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Final 4 Aggregate Repositories (Loyalty, Auction, Bundle, DigitalProduct)
builder.Services.AddScoped<ILoyaltyAccountRepository, LoyaltyAccountRepository>();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IBundleRepository, BundleRepository>();
builder.Services.AddScoped<IDigitalProductRepository, DigitalProductRepository>();

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
            
            AGGREGATES (20 total):
            - Order, Customer, Product, Payment, Shipment
            - Cart, Promotion, Review, Return, Inventory
            - Wishlist, Subscription, GiftCard, Vendor
            - Tax Configuration, Currency Configuration
            - Loyalty Program, Auction, Bundle, Digital Product
            
            PATTERNS:
            - Rich Domain Model with business rules
            - CQRS (Commands/Queries separation)
            - Domain Events for decoupled integrations
            - Repository Pattern (interface in Domain)
            - Clean Architecture layers
            
            Try the endpoints to see DDD in action!
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
    ║  AGGREGATES (20 total):                                                   ║
    ║  ──────────────────────                                                   ║
    ║  • Order          • Customer        • Product        • Payment            ║
    ║  • Shipment       • Cart            • Promotion      • Review             ║
    ║  • Return         • Inventory       • Wishlist       • Subscription       ║
    ║  • GiftCard       • Vendor          • Tax            • Currency           ║
    ║  • Loyalty        • Auction         • Bundle         • DigitalProduct     ║
    ║                                                                           ║
    ║  PATTERNS DEMONSTRATED:                                                   ║
    ║  ─────────────────────                                                    ║
    ║  ✓ Rich Domain Model (aggregates with business rules)                     ║
    ║  ✓ Value Objects (Money, Address - immutable)                             ║
    ║  ✓ CQRS (Commands and Queries separation)                                 ║
    ║  ✓ Domain Events (50+ events for all aggregates)                          ║
    ║  ✓ Repository Pattern (interface in Domain)                               ║
    ║  ✓ Clean Architecture layers                                              ║
    ║                                                                           ║
    ║  API ENDPOINTS (20 controllers):                                          ║
    ║  ───────────────────────────────                                          ║
    ║  /api/orders           - Order management                                 ║
    ║  /api/cart             - Shopping cart                                    ║
    ║  /api/products         - Product catalog                                  ║
    ║  /api/customers        - Customer profiles                                ║
    ║  /api/payments         - Payment processing                               ║
    ║  /api/shipments        - Shipping & tracking                              ║
    ║  /api/returns          - Return requests                                  ║
    ║  /api/reviews          - Product reviews                                  ║
    ║  /api/promotions       - Promo codes                                      ║
    ║  /api/wishlists        - Customer wishlists                               ║
    ║  /api/subscriptions    - Recurring orders                                 ║
    ║  /api/giftcards        - Gift cards & store credit                        ║
    ║  /api/vendors          - Marketplace sellers                              ║
    ║  /api/tax              - Tax configuration                                ║
    ║  /api/currency         - Multi-currency support                           ║
    ║  /api/loyalty          - Points & rewards program                         ║
    ║  /api/auctions         - Bidding system                                   ║
    ║  /api/bundles          - Product bundling                                 ║
    ║  /api/digital-products - Downloads & licenses                             ║
    ║                                                                           ║
    ║  Open Swagger UI at: https://localhost:5001/swagger                       ║
    ║                                                                           ║
    ╚═══════════════════════════════════════════════════════════════════════════╝
    
    """);

app.Run();
