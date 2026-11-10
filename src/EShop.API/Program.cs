using EShop.Application.Commands.Orders;
using EShop.Application.Interfaces;
using EShop.Application.Services;
using EShop.Domain.Repositories;
using EShop.Infrastructure;
using EShop.Infrastructure.DomainEvents;
using EShop.Infrastructure.Email;
using EShop.Infrastructure.Identity;
using EShop.Infrastructure.Payments.Stripe;
using EShop.Infrastructure.Repositories;
using EShop.Infrastructure.Services;
using Microsoft.OpenApi.Models;

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
// AUTHENTICATION & AUTHORIZATION
// ───────────────────────────────────────────────────────────────────────────

builder.Services.AddIdentityServices(builder.Configuration);

// ───────────────────────────────────────────────────────────────────────────
// PAYMENT GATEWAY - STRIPE
// ───────────────────────────────────────────────────────────────────────────

builder.Services.AddStripePayments(builder.Configuration);

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
    ║  🔐 AUTHENTICATION FLOW:                                                  ║
    ║  ───────────────────────                                                  ║
    ║  POST /api/auth/register     - Create new account                         ║
    ║  POST /api/auth/login        - Login with email/password                  ║
    ║  POST /api/auth/refresh      - Refresh access token                       ║
    ║  POST /api/auth/external-login - OAuth (Google, Facebook, etc.)           ║
    ║  POST /api/auth/logout       - Revoke refresh token                       ║
    ║  GET  /api/auth/me           - Get current user info                      ║
    ║                                                                           ║
    ║  Open Swagger UI at: https://localhost:5001/swagger                       ║
    ║                                                                           ║
    ╚═══════════════════════════════════════════════════════════════════════════╝
    
    """);

app.Run();
