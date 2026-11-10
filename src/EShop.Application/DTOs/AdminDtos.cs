namespace EShop.Application.DTOs;

/// <summary>
/// Generic paged result for admin queries
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

/// <summary>
/// Result of bulk operations
/// </summary>
public class BulkOperationResultDto
{
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<BulkOperationError> Errors { get; init; } = Array.Empty<BulkOperationError>();

    public static BulkOperationResultDto Success(int count) => new() { SuccessCount = count };

    public static BulkOperationResultDto WithErrors(int successCount, IReadOnlyList<BulkOperationError> errors)
        => new() { SuccessCount = successCount, FailedCount = errors.Count, Errors = errors };
}

public class BulkOperationError
{
    public string ItemId { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

#region Dashboard DTOs

public class DashboardOverviewDto
{
    public decimal TotalRevenue { get; init; }
    public decimal RevenueChange { get; init; }
    public int TotalOrders { get; init; }
    public decimal OrdersChange { get; init; }
    public int TotalCustomers { get; init; }
    public decimal CustomersChange { get; init; }
    public int TotalProducts { get; init; }
    public decimal ProductsChange { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal ConversionRate { get; init; }
}

public class SalesAnalyticsDto
{
    public IReadOnlyList<SalesDataPointDto> DailySales { get; init; } = Array.Empty<SalesDataPointDto>();
    public IReadOnlyList<SalesDataPointDto> WeeklySales { get; init; } = Array.Empty<SalesDataPointDto>();
    public IReadOnlyList<SalesDataPointDto> MonthlySales { get; init; } = Array.Empty<SalesDataPointDto>();
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public decimal AverageOrderValue { get; init; }
}

public class SalesDataPointDto
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}

public class TopProductDto
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
}

public class RecentOrderDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public class RecentActivityDto
{
    public string ActivityId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public class InventoryAlertDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int ReorderLevel { get; init; }
    public string AlertType { get; init; } = string.Empty; // LowStock, OutOfStock, Overstock
    public string Severity { get; init; } = string.Empty; // Low, Medium, High, Critical
}

public class SystemHealthDto
{
    public string Status { get; init; } = string.Empty; // Healthy, Degraded, Unhealthy
    public double CpuUsage { get; init; }
    public double MemoryUsage { get; init; }
    public double DiskUsage { get; init; }
    public IReadOnlyList<ServiceHealthDto> Services { get; init; } = Array.Empty<ServiceHealthDto>();
    public DateTime LastUpdated { get; init; }
}

public class ServiceHealthDto
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double ResponseTimeMs { get; init; }
    public string? ErrorMessage { get; init; }
}

#endregion

#region User DTOs

public class AdminUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime? LockoutEnd { get; init; }
}

public class AdminUserDetailDto : AdminUserDto
{
    public IReadOnlyList<UserSessionDto> ActiveSessions { get; init; } = Array.Empty<UserSessionDto>();
    public IReadOnlyList<UserActivityLogDto> RecentActivity { get; init; } = Array.Empty<UserActivityLogDto>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

public class UserSessionDto
{
    public string SessionId { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
}

public class UserActivityLogDto
{
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public class CreateUserRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public bool SendWelcomeEmail { get; init; } = true;
}

public class UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? IsActive { get; init; }
    public IReadOnlyList<string>? Roles { get; init; }
}

public class UserRoleUpdateRequest
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

#endregion

#region Order DTOs

public class AdminOrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal Tax { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string? PaymentMethod { get; init; }
    public string? ShippingMethod { get; init; }
    public string? TrackingNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public int ItemCount { get; init; }
}

public class AdminOrderDetailDto : AdminOrderDto
{
    public IReadOnlyList<AdminOrderItemDto> Items { get; init; } = Array.Empty<AdminOrderItemDto>();
    public AdminAddressDto? ShippingAddress { get; init; }
    public AdminAddressDto? BillingAddress { get; init; }
    public IReadOnlyList<OrderStatusHistoryDto> StatusHistory { get; init; } = Array.Empty<OrderStatusHistoryDto>();
    public IReadOnlyList<OrderNoteDto> Notes { get; init; } = Array.Empty<OrderNoteDto>();
    public string? CancellationReason { get; init; }
    public string? RefundReason { get; init; }
}

public class AdminOrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Total { get; init; }
    public string? ImageUrl { get; init; }
}

public class AdminAddressDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
}

public class OrderStatusHistoryDto
{
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public class OrderNoteDto
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsInternal { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
    public bool NotifyCustomer { get; init; } = true;
}

public class AddOrderNoteRequest
{
    public string Content { get; init; } = string.Empty;
    public bool IsInternal { get; init; } = true;
}

public class ProcessRefundRequest
{
    public decimal? Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool RestockItems { get; init; } = true;
    public bool NotifyCustomer { get; init; } = true;
}

public class UpdateShippingRequest
{
    public string? TrackingNumber { get; init; }
    public string? Carrier { get; init; }
    public bool NotifyCustomer { get; init; } = true;
}

public class OrderStatisticsDto
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public int ProcessingOrders { get; init; }
    public int ShippedOrders { get; init; }
    public int DeliveredOrders { get; init; }
    public int CancelledOrders { get; init; }
    public int RefundedOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal RefundedAmount { get; init; }
}

#endregion

#region Product DTOs

public class AdminProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? Cost { get; init; }
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity => StockQuantity - ReservedQuantity;
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public string? CategoryName { get; init; }
    public Guid? CategoryId { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class AdminProductDetailDto : AdminProductDto
{
    public IReadOnlyList<ProductImageDto> Images { get; init; } = Array.Empty<ProductImageDto>();
    public IReadOnlyList<ProductVariantDto> Variants { get; init; } = Array.Empty<ProductVariantDto>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public ProductSeoDto? Seo { get; init; }
    public IReadOnlyList<ProductReviewSummaryDto> Reviews { get; init; } = Array.Empty<ProductReviewSummaryDto>();
    public ProductInventoryDto Inventory { get; init; } = new();
    public decimal? Weight { get; init; }
    public string? WeightUnit { get; init; }
    public ProductDimensionsDto? Dimensions { get; init; }
}

public class ProductImageDto
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? AltText { get; init; }
    public int Position { get; init; }
    public bool IsPrimary { get; init; }
}

public class ProductVariantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
}

public class ProductSeoDto
{
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? Slug { get; init; }
}

public class ProductReviewSummaryDto
{
    public double AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public IReadOnlyDictionary<int, int> RatingDistribution { get; init; } = new Dictionary<int, int>();
}

public class ProductInventoryDto
{
    public int StockQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity => StockQuantity - ReservedQuantity;
    public int ReorderLevel { get; init; }
    public int ReorderQuantity { get; init; }
    public bool TrackInventory { get; init; }
    public bool AllowBackorder { get; init; }
}

public class ProductDimensionsDto
{
    public decimal? Length { get; init; }
    public decimal? Width { get; init; }
    public decimal? Height { get; init; }
    public string? Unit { get; init; }
}

public class CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? Cost { get; init; }
    public int StockQuantity { get; init; }
    public Guid? CategoryId { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<string>? Tags { get; init; }
    public ProductSeoDto? Seo { get; init; }
}

public class UpdateProductRequest
{
    public string? Name { get; init; }
    public string? Sku { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? Cost { get; init; }
    public Guid? CategoryId { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsFeatured { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public ProductSeoDto? Seo { get; init; }
}

public class UpdateInventoryRequest
{
    public int Quantity { get; init; }
    public string AdjustmentType { get; init; } = string.Empty; // Set, Add, Subtract
    public string? Reason { get; init; }
}

public class BulkPriceUpdateRequest
{
    public IReadOnlyList<Guid> ProductIds { get; init; } = Array.Empty<Guid>();
    public decimal? FixedPrice { get; init; }
    public decimal? PercentageChange { get; init; }
    public bool RoundToNearest { get; init; }
    public decimal? RoundingValue { get; init; }
}

public class ProductStatisticsDto
{
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int InactiveProducts { get; init; }
    public int OutOfStockProducts { get; init; }
    public int LowStockProducts { get; init; }
    public int FeaturedProducts { get; init; }
    public decimal TotalInventoryValue { get; init; }
    public decimal AveragePrice { get; init; }
}

public class AdminCategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Slug { get; init; }
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public int ProductCount { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}

#endregion

#region Customer DTOs

public class AdminCustomerDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
    public bool IsVerified { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageOrderValue { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastOrderAt { get; init; }
    public string? CustomerTier { get; init; } // Bronze, Silver, Gold, Platinum
}

public class AdminCustomerDetailDto : AdminCustomerDto
{
    public IReadOnlyList<CustomerAddressDto> Addresses { get; init; } = Array.Empty<CustomerAddressDto>();
    public IReadOnlyList<CustomerOrderSummaryDto> RecentOrders { get; init; } = Array.Empty<CustomerOrderSummaryDto>();
    public CustomerPreferencesDto? Preferences { get; init; }
    public IReadOnlyList<CustomerNoteDto> Notes { get; init; } = Array.Empty<CustomerNoteDto>();
    public CustomerLoyaltyDto? Loyalty { get; init; }
}

public class CustomerAddressDto
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty; // Home, Work, etc.
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public class CustomerOrderSummaryDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public class CustomerPreferencesDto
{
    public bool ReceiveMarketing { get; init; }
    public string? PreferredLanguage { get; init; }
    public string? PreferredCurrency { get; init; }
}

public class CustomerNoteDto
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public class CustomerLoyaltyDto
{
    public int Points { get; init; }
    public string Tier { get; init; } = string.Empty;
    public int PointsToNextTier { get; init; }
    public DateTime? TierExpiresAt { get; init; }
}

public class AddCustomerNoteRequest
{
    public string Content { get; init; } = string.Empty;
}

public class UpdateCustomerRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? IsActive { get; init; }
    public CustomerPreferencesDto? Preferences { get; init; }
}

public class CustomerStatisticsDto
{
    public int TotalCustomers { get; init; }
    public int ActiveCustomers { get; init; }
    public int NewCustomersThisMonth { get; init; }
    public int ReturningCustomers { get; init; }
    public decimal AverageLifetimeValue { get; init; }
    public decimal AverageOrderValue { get; init; }
    public IReadOnlyDictionary<string, int> CustomersByTier { get; init; } = new Dictionary<string, int>();
}

#endregion

#region Settings DTOs

public class GeneralSettingsDto
{
    public string StoreName { get; init; } = string.Empty;
    public string? StoreDescription { get; init; }
    public string? LogoUrl { get; init; }
    public string? FaviconUrl { get; init; }
    public string DefaultCurrency { get; init; } = "USD";
    public string DefaultLanguage { get; init; } = "en";
    public string TimeZone { get; init; } = "UTC";
    public string? SupportEmail { get; init; }
    public string? SupportPhone { get; init; }
}

public class PaymentSettingsDto
{
    public bool StripeEnabled { get; init; }
    public bool PayPalEnabled { get; init; }
    public bool CodEnabled { get; init; }
    public IReadOnlyList<string> AcceptedCurrencies { get; init; } = Array.Empty<string>();
    public bool AutoCapturePayments { get; init; }
}

public class ShippingSettingsDto
{
    public bool FreeShippingEnabled { get; init; }
    public decimal? FreeShippingThreshold { get; init; }
    public decimal DefaultShippingRate { get; init; }
    public IReadOnlyList<ShippingZoneDto> ShippingZones { get; init; } = Array.Empty<ShippingZoneDto>();
}

public class ShippingZoneDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> Countries { get; init; } = Array.Empty<string>();
    public decimal Rate { get; init; }
}

public class TaxSettingsDto
{
    public bool TaxEnabled { get; init; }
    public bool PricesIncludeTax { get; init; }
    public decimal DefaultTaxRate { get; init; }
    public IReadOnlyList<TaxRateDto> TaxRates { get; init; } = Array.Empty<TaxRateDto>();
}

public class TaxRateDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? State { get; init; }
    public decimal Rate { get; init; }
}

public class NotificationSettingsDto
{
    public bool OrderConfirmationEnabled { get; init; }
    public bool ShippingNotificationEnabled { get; init; }
    public bool DeliveryNotificationEnabled { get; init; }
    public bool AbandonedCartEmailEnabled { get; init; }
    public int AbandonedCartDelayHours { get; init; }
    public bool LowStockAlertEnabled { get; init; }
    public int LowStockThreshold { get; init; }
}

public class EmailTemplateDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public class UpdateEmailTemplateRequest
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public class AuditLogDto
{
    public string Id { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

#endregion


#region Type Aliases for Naming Compatibility

// Alias DTOs for controllers that use 'Details' vs 'Detail' naming
/// <summary>Alias for AdminCustomerDetailDto (controller compatibility)</summary>
public class AdminCustomerDetailsDto : AdminCustomerDetailDto { }

/// <summary>Alias for AdminOrderDetailDto (controller compatibility)</summary>
public class AdminOrderDetailsDto : AdminOrderDetailDto { }

/// <summary>Alias for AdminProductDetailDto (controller compatibility)</summary>
public class AdminProductDetailsDto : AdminProductDetailDto { }

/// <summary>Alias for AdminUserDetailDto (controller compatibility)</summary>
public class AdminUserDetailsDto : AdminUserDetailDto { }

#endregion

#region Additional Dashboard DTOs

public class OrderStatsDto
{
    public int TotalOrders { get; init; }
    public int OrdersToday { get; init; }
    public int OrdersThisWeek { get; init; }
    public int OrdersThisMonth { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal RevenueToday { get; init; }
    public decimal RevenueThisWeek { get; init; }
    public decimal RevenueThisMonth { get; init; }
    public decimal AverageOrderValue { get; init; }
    public IReadOnlyDictionary<string, int> OrdersByStatus { get; init; } = new Dictionary<string, int>();
}

public class CustomerStatsDto
{
    public int TotalCustomers { get; init; }
    public int NewCustomersToday { get; init; }
    public int NewCustomersThisWeek { get; init; }
    public int NewCustomersThisMonth { get; init; }
    public int ActiveCustomers { get; init; }
    public decimal AverageLifetimeValue { get; init; }
    public IReadOnlyDictionary<string, int> CustomersByTier { get; init; } = new Dictionary<string, int>();
}

public class LowStockProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int ReorderLevel { get; init; }
    public string? ImageUrl { get; init; }
}

public class TopCustomerDto
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
    public string? CustomerTier { get; init; }
}

public class PaymentMethodRevenueDto
{
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int TransactionCount { get; init; }
    public decimal Percentage { get; init; }
}

public class CategoryRevenueDto
{
    public Guid? CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int ItemsSold { get; init; }
    public decimal Percentage { get; init; }
}

public class RealtimeMetricsDto
{
    public int ActiveUsers { get; init; }
    public int ActiveCarts { get; init; }
    public decimal CartValue { get; init; }
    public int OrdersInProgress { get; init; }
    public int PendingPayments { get; init; }
    public double ServerLoad { get; init; }
    public DateTime LastUpdated { get; init; }
}

#endregion

#region Additional Customer DTOs

public class WalletTransactionDto
{
    public string TransactionId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty; // Credit, Debit
    public string Description { get; init; } = string.Empty;
    public decimal BalanceAfter { get; init; }
    public DateTime Timestamp { get; init; }
}

public class LoyaltyTransactionDto
{
    public string TransactionId { get; init; } = string.Empty;
    public int Points { get; init; }
    public string Type { get; init; } = string.Empty; // Earned, Redeemed, Expired, Bonus
    public string Description { get; init; } = string.Empty;
    public int BalanceAfter { get; init; }
    public DateTime Timestamp { get; init; }
}

public class CustomerSegmentDto
{
    public string SegmentId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int CustomerCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
}

#endregion

#region Additional Product DTOs

public class ProductSalesHistoryDto
{
    public DateTime Date { get; init; }
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}

public class StockMovementDto
{
    public string MovementId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Type { get; init; } = string.Empty; // In, Out, Adjustment
    public int Quantity { get; init; }
    public int BalanceAfter { get; init; }
    public string? Reference { get; init; }
    public string? Reason { get; init; }
    public string? PerformedBy { get; init; }
}

#endregion

#region Additional Order DTOs

public class OrderTimelineEventDto
{
    public string EventId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? PerformedBy { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public class RefundResultDto
{
    public bool Success { get; init; }
    public string? RefundId { get; init; }
    public decimal AmountRefunded { get; init; }
    public string? ErrorMessage { get; init; }
}

#endregion

#region Additional User DTOs

public class UserActivityDto
{
    public string ActivityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
}

public class ImpersonationResultDto
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? ErrorMessage { get; init; }
}

public class PasswordResetResultDto
{
    public bool Success { get; init; }
    public bool EmailSent { get; init; }
    public string? ErrorMessage { get; init; }
}

#endregion
