using EShop.API.Configuration;
using EShop.API.Middleware;
using EShop.Application.Behaviors;
using EShop.Application.Commands.Orders;
using EShop.Application.Interfaces;
using EShop.Application.Services;
using EShop.Application.Validation;
using EShop.Domain.Repositories;
using EShop.Infrastructure;
using EShop.Infrastructure.DomainEvents;
using EShop.Infrastructure.Email;
using EShop.Infrastructure.Health;
using EShop.Infrastructure.Identity;
using EShop.Infrastructure.Payments.Stripe;
using EShop.Infrastructure.Persistence;
using EShop.Infrastructure.Repositories;
using EShop.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// ═══════════════════════════════════════════════════════════════════════════
// SERILOG BOOTSTRAP - Must be first for early logging
// ═══════════════════════════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogLogging();

// ═══════════════════════════════════════════════════════════════════════════
// CROSS-CUTTING CONCERNS
// ═══════════════════════════════════════════════════════════════════════════

// Global Exception Handler (RFC 7807 Problem Details)
builder.Services.AddGlobalExceptionHandler();

// Response Compression (Brotli/Gzip)
builder.Services.AddCompressionConfiguration();

// CORS Configuration
builder.Services.AddCorsConfiguration(builder.Configuration);

// API Versioning
builder.Services.AddApiVersioningConfiguration();

// Health Checks
builder.Services.AddHealthCheckServices(builder.Configuration);

// Rate Limiting (NEW)
builder.Services.AddRateLimitingConfiguration(builder.Configuration);

// Output Cache (Response Caching)
builder.Services.AddOutputCacheConfiguration();

// Audit Interceptor for automatic audit trail
builder.Services.AddAuditInterceptor();

// ═══════════════════════════════════════════════════════════════════════════
// DEPENDENCY INJECTION - Following DDD + Clean Architecture principles
// ═══════════════════════════════════════════════════════════════════════════

// MediatR for CQRS and domain events (scans Application assembly for all handlers)
builder.Services.AddMediatR(cfg => 
{
    // Register ALL handlers from Application layer (Commands, Queries, Events)
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly);
    
    // Add pipeline behaviors for cross-cutting concerns
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
});

// FluentValidation - Auto-register all validators from Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

// ───────────────────────────────────────────────────────────────────────────
// DOMAIN LAYER - No dependencies to register (pure C# classes)
// ───────────────────────────────────────────────────────────────────────────

// ───────────────────────────────────────────────────────────────────────────
// APPLICATION LAYER - Application Services
// ───────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderAppService, OrderAppService>();

// ───────────────────────────────────────────────────────────────────────────
// AUTHENTICATION & AUTHORIZATION
// ───────────────────────────────────────────────────────────────────────────

builder.Services.AddIdentityServices(builder.Configuration);

// ───────────────────────────────────────────────────────────────────────────
// PAYMENT GATEWAY - STRIPE
// ───────────────────────────────────────────────────────────────────────────

builder.Services.AddStripePayments(builder.Configuration);

// ───────────────────────────────────────────────────────────────────────────
// INFRASTRUCTURE LAYER - Database Context (EF Core)
// ───────────────────────────────────────────────────────────────────────────

// Register EShopDbContext - uses PostgreSQL if available, falls back to InMemory
var connectionString = builder.Configuration.GetConnectionString("Database");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<EShopDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(30);
        });
    });
}
else
{
    // Fallback to InMemory database for local development without PostgreSQL
    builder.Services.AddDbContext<EShopDbContext>(options =>
    {
        options.UseInMemoryDatabase("EShopLocalDb");
    });
}

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

// Additional 7 Aggregate Repositories (Affiliate, Coupon, Catalog, Analytics, Content, Support, Supplier)
builder.Services.AddScoped<IAffiliateRepository, AffiliateRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISalesReportRepository, SalesReportRepository>();
builder.Services.AddScoped<ICustomerInsightRepository, CustomerInsightRepository>();
builder.Services.AddScoped<IProductPerformanceRepository, ProductPerformanceRepository>();
builder.Services.AddScoped<IBannerRepository, BannerRepository>();
builder.Services.AddScoped<ILandingPageRepository, LandingPageRepository>();
builder.Services.AddScoped<IFaqRepository, FaqRepository>();
builder.Services.AddScoped<IBlogPostRepository, BlogPostRepository>();
builder.Services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

// Domain Event Dispatcher
builder.Services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

// External Services (fakes for demo)
// Email: Configured via AddEmailServices - uses FakeEmailService by default, or SendGrid/SES/SMTP based on config
builder.Services.AddEmailServices(builder.Configuration);
builder.Services.AddSingleton<IInventoryService, FakeInventoryService>();
builder.Services.AddSingleton<IPaymentService, FakePaymentService>();

// ───────────────────────────────────────────────────────────────────────────
// ADMIN DASHBOARD SERVICES
// ───────────────────────────────────────────────────────────────────────────
builder.Services.AddAdminServices();

// ───────────────────────────────────────────────────────────────────────────
// ASP.NET Core / Presentation Layer
// ───────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings in JSON
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "EShop DDD Demo API",
        Version = "v1",
        Description = """
            A complete Domain-Driven Design demo with:
            
            AGGREGATES (28 total):
            - Identity (User, Roles, RefreshTokens, ExternalLogins)
            - Order, Customer, Product, Payment, Shipment
            - Cart, Promotion, Review, Return, Inventory
            - Wishlist, Subscription, GiftCard, Vendor
            - Tax Configuration, Currency Configuration
            - Loyalty Program, Auction, Bundle, Digital Product
            - Affiliate, Coupon, Catalog/Category, Analytics
            - Content (Banner, FAQ, Blog), Support, Supplier
            
            PATTERNS:
            - Rich Domain Model with business rules
            - CQRS (Commands/Queries separation)
            - Domain Events for decoupled integrations
            - Repository Pattern (interface in Domain)
            - Clean Architecture layers
            - JWT Authentication with Refresh Tokens
            - Role-Based Authorization
            
            Try the endpoints to see DDD in action!
            """
    });
    
    // Handle duplicate type names by using full name
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

// ═══════════════════════════════════════════════════════════════════════════
// CONFIGURE PIPELINE
// ═══════════════════════════════════════════════════════════════════════════

var app = builder.Build();

// Global Exception Handler (first in pipeline to catch all errors)
app.UseGlobalExceptionHandler();

// Serilog Request Logging (after exception handler)
app.UseSerilogRequestLoggingMiddleware();

// Response Compression
app.UseResponseCompression();

// Rate Limiting (before other middleware to protect resources)
app.UseRateLimitingMiddleware();

// Output Cache (after rate limiting, before endpoints)
app.UseOutputCacheMiddleware();

// Health Check Endpoints (before auth so they're publicly accessible)
app.UseHealthCheckEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EShop DDD Demo v1");
        c.DocumentTitle = "DDD Demo API";
    });
    
    // Use permissive CORS in development
    app.UseCors("Development");
}
else
{
    // Use configured CORS policy in production
    app.UseCors(CorsConfiguration.PolicyName);
}

app.UseHttpsRedirection();
app.UseAuthentication();
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
    ║  AGGREGATES (28 total):                                                   ║
    ║  ──────────────────────                                                   ║
    ║  • Identity       • Order          • Customer        • Product            ║
    ║  • Payment        • Shipment       • Cart            • Promotion          ║
    ║  • Review         • Return         • Inventory       • Wishlist           ║
    ║  • Subscription   • GiftCard       • Vendor          • Tax                ║
    ║  • Currency       • Loyalty        • Auction         • Bundle             ║
    ║  • DigitalProduct • Affiliate      • Coupon          • Catalog            ║
    ║  • Analytics      • Content        • Support         • Supplier           ║
    ║                                                                           ║
    ║  PRODUCTION FEATURES:                                                     ║
    ║  ─────────────────────                                                    ║
    ║  ✓ Global Exception Handler (RFC 7807 Problem Details)                    ║
    ║  ✓ Response Compression (Brotli/Gzip)                                     ║
    ║  ✓ CORS Configuration                                                     ║
    ║  ✓ API Versioning                                                         ║
    ║  ✓ Health Checks (/health/live, /health/ready)                            ║
    ║  ✓ API Rate Limiting (Fixed/Sliding Window, Token Bucket)     [NEW]       ║
    ║  ✓ Structured Logging (Serilog + Seq)                         [NEW]       ║
    ║  ✓ FluentValidation (Command/Query validation)                [NEW]       ║
    ║  ✓ Audit Trail (CreatedBy, ModifiedBy, DeletedBy)            [NEW]       ║
    ║                                                                           ║
    ║  PATTERNS DEMONSTRATED:                                                   ║
    ║  ─────────────────────                                                    ║
    ║  ✓ Rich Domain Model (aggregates with business rules)                     ║
    ║  ✓ Value Objects (Money, Address - immutable)                             ║
    ║  ✓ CQRS (Commands and Queries separation)                                 ║
    ║  ✓ Domain Events (70+ events for all aggregates)                          ║
    ║  ✓ Repository Pattern (interface in Domain)                               ║
    ║  ✓ Clean Architecture layers                                              ║
    ║  ✓ JWT Authentication with Refresh Token Rotation                         ║
    ║  ✓ Role-Based Authorization (Admin, Customer, Vendor)                     ║
    ║  ✓ OAuth Support (Google, Facebook, Apple, Microsoft)                     ║
    ║  ✓ MediatR Pipeline Behaviors (Validation, Logging, Performance)          ║
    ║                                                                           ║
    ║  HEALTH ENDPOINTS:                                                        ║
    ║  ─────────────────                                                        ║
    ║  GET /health       - Full health status                                   ║
    ║  GET /health/live  - Liveness probe (K8s)                                 ║
    ║  GET /health/ready - Readiness probe (K8s)                                ║
    ║                                                                           ║
    ║  RATE LIMITING POLICIES:                                                  ║
    ║  ───────────────────────                                                  ║
    ║  • fixed          - 100 req/min (general API)                             ║
    ║  • sliding        - 100 req/min (smooth distribution)                     ║
    ║  • token-bucket   - 50 tokens, 10/sec refill (burst support)              ║
    ║  • auth           - 10 req/min (brute force protection)                   ║
    ║  • payment        - 5 req/min (fraud protection)                          ║
    ║  • per-user       - 100 req/min per user/IP                               ║
    ║                                                                           ║
    ║  API ENDPOINTS (28 controllers):                                          ║
    ║  ───────────────────────────────                                          ║
    ║  /api/auth             - 🔐 Authentication & Authorization                ║
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
    ║  /api/affiliates       - Affiliate marketing program                      ║
    ║  /api/coupons          - Coupon management                                ║
    ║  /api/categories       - Product categories                               ║
    ║  /api/analytics        - Sales & customer analytics                       ║
    ║  /api/content          - Banners, FAQ, Blog posts                         ║
    ║  /api/support          - Support tickets                                  ║
    ║  /api/suppliers        - Supplier & purchase orders                       ║
    ║                                                                           ║
    ║  Open Swagger UI at: https://localhost:5001/swagger                       ║
    ║                                                                           ║
    ╚═══════════════════════════════════════════════════════════════════════════╝
    
    """);

app.Run();
