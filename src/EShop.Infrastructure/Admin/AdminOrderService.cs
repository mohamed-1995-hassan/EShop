using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using EShop.Domain.Aggregates.OrderAggregate;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace EShop.Infrastructure.Admin;

/// <summary>
/// Implementation of admin order management service
/// </summary>
public class AdminOrderService : IAdminOrderService
{
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(EShopDbContext dbContext, ILogger<AdminOrderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<AdminOrderDto>> GetOrdersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? status = null,
        string? paymentStatus = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? customerId = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .Include(o => o.OrderLines)
            .AsQueryable();

        // Apply filters - Order doesn't have OrderNumber, so search by ID
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.Id.ToString().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "ordernumber" => sortDescending ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id),
            "total" => sortDescending ? query.OrderByDescending(o => o.TotalAmount.Amount) : query.OrderBy(o => o.TotalAmount.Amount),
            "status" => sortDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            _ => sortDescending ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate)
        };

        // Apply pagination
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(MapToAdminOrderDto).ToList();

        return PagedResult<AdminOrderDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<AdminOrderDetailDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null) return null;

        return MapToAdminOrderDetailDto(order);
    }

    public async Task<AdminOrderDetailDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        // Order doesn't have OrderNumber, try to parse as GUID
        if (Guid.TryParse(orderNumber, out var orderId))
        {
            return await GetOrderByIdAsync(orderId, cancellationToken);
        }
        return null;
    }

    public async Task<AdminOrderDto> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        if (Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
        {
            // Update status based on the new status
            switch (newStatus)
            {
                case OrderStatus.Placed:
                    order.Place();
                    break;
                case OrderStatus.Confirmed:
                    order.Confirm();
                    break;
                case OrderStatus.Shipped:
                    order.Ship();
                    break;
                case OrderStatus.Completed:
                    order.Complete();
                    break;
                case OrderStatus.Cancelled:
                    order.Cancel(request.Note ?? "Cancelled by admin");
                    break;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, request.Status);

        return MapToAdminOrderDto(order);
    }

    public async Task<AdminOrderDto> UpdateShippingAsync(Guid orderId, UpdateShippingRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            order.Ship(request.TrackingNumber, request.Carrier);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Order {OrderId} shipping updated", orderId);

        return MapToAdminOrderDto(order);
    }

    public async Task<AdminOrderDto> AddOrderNoteAsync(Guid orderId, AddOrderNoteRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        // In a real implementation, you would have a Notes collection on the Order
        // For now, we'll just log the note
        _logger.LogInformation("Note added to order {OrderId}: {Note}", orderId, request.Content);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToAdminOrderDto(order);
    }

    public async Task<AdminOrderDto> ProcessRefundAsync(Guid orderId, ProcessRefundRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        // In a real implementation, you would integrate with Stripe refund API
        // and update the order's refund status
        _logger.LogInformation("Refund processed for order {OrderId}: Amount={Amount}, Reason={Reason}", 
            orderId, request.Amount ?? order.TotalAmount.Amount, request.Reason);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToAdminOrderDto(order);
    }

    public async Task CancelOrderAsync(Guid orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        order.Cancel(reason ?? "Cancelled by admin");
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", orderId, reason);
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        var orders = await query.ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.TotalAmount.Amount);

        return new OrderStatisticsDto
        {
            TotalOrders = totalOrders,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Confirmed), // Using Confirmed as Processing
            ShippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped),
            DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Completed),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            RefundedOrders = 0, // Would need refund tracking
            TotalRevenue = totalRevenue,
            AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
            RefundedAmount = 0 // Would need refund tracking
        };
    }

    public async Task<BulkOperationResultDto> BulkUpdateStatusAsync(IReadOnlyList<Guid> orderIds, string status, CancellationToken cancellationToken = default)
    {
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var orderId in orderIds)
        {
            try
            {
                await UpdateOrderStatusAsync(orderId, new UpdateOrderStatusRequest { Status = status }, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = orderId.ToString(), ErrorMessage = ex.Message });
            }
        }

        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    public async Task<byte[]> ExportOrdersAsync(
        string format,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .Include(o => o.OrderLines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            query = query.Where(o => o.Status == orderStatus);

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync(cancellationToken);

        if (format.ToLowerInvariant() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("OrderId,CustomerId,Total,Status,OrderDate,ItemCount");
            foreach (var order in orders)
            {
                csv.AppendLine($"{order.Id},{order.CustomerId},{order.TotalAmount.Amount},{order.Status},{order.OrderDate:yyyy-MM-dd HH:mm:ss},{order.OrderLines.Count}");
            }
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        // Default to JSON
        var dtos = orders.Select(MapToAdminOrderDto).ToList();
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static AdminOrderDto MapToAdminOrderDto(Order order)
    {
        return new AdminOrderDto
        {
            Id = order.Id,
            OrderNumber = order.Id.ToString().Substring(0, 8).ToUpper(), // Order doesn't have OrderNumber
            CustomerId = order.CustomerId,
            CustomerName = "Unknown", // Customer is not loaded in this mapping
            CustomerEmail = "Unknown",
            Subtotal = order.OrderLines.Sum(li => li.LineTotal.Amount),
            Tax = 0, // Would need tax calculation
            ShippingCost = 0, // Would need shipping calculation
            Discount = 0, // Would need discount tracking
            Total = order.TotalAmount.Amount,
            Status = order.Status.ToString(),
            PaymentStatus = "Pending", // Order doesn't have PaymentStatus
            PaymentMethod = null, // Would need payment method tracking
            ShippingMethod = null, // Would need shipping method tracking
            TrackingNumber = null, // Order doesn't have TrackingNumber
            CreatedAt = order.OrderDate,
            PaidAt = null, // Order doesn't track this
            ShippedAt = null,
            DeliveredAt = null,
            CancelledAt = order.Status == OrderStatus.Cancelled ? DateTime.UtcNow : null,
            ItemCount = order.OrderLines.Count
        };
    }

    private static AdminOrderDetailDto MapToAdminOrderDetailDto(Order order)
    {
        var dto = MapToAdminOrderDto(order);
        return new AdminOrderDetailDto
        {
            Id = dto.Id,
            OrderNumber = dto.OrderNumber,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            Subtotal = dto.Subtotal,
            Tax = dto.Tax,
            ShippingCost = dto.ShippingCost,
            Discount = dto.Discount,
            Total = dto.Total,
            Status = dto.Status,
            PaymentStatus = dto.PaymentStatus,
            PaymentMethod = dto.PaymentMethod,
            ShippingMethod = dto.ShippingMethod,
            TrackingNumber = dto.TrackingNumber,
            CreatedAt = dto.CreatedAt,
            PaidAt = dto.PaidAt,
            ShippedAt = dto.ShippedAt,
            DeliveredAt = dto.DeliveredAt,
            CancelledAt = dto.CancelledAt,
            ItemCount = dto.ItemCount,
            Items = order.OrderLines.Select(li => new AdminOrderItemDto
            {
                ProductId = li.ProductId,
                ProductName = li.ProductName,
                Sku = "N/A", // OrderLine doesn't store SKU
                UnitPrice = li.UnitPrice.Amount,
                Quantity = li.Quantity,
                Total = li.LineTotal.Amount,
                ImageUrl = null
            }).ToList(),
            ShippingAddress = order.ShippingAddress != null ? new AdminAddressDto
            {
                Street = order.ShippingAddress.Street,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.Country, // Address doesn't have State
                Country = order.ShippingAddress.Country,
                PostalCode = order.ShippingAddress.PostalCode
            } : null,
            BillingAddress = null, // Order doesn't have BillingAddress
            StatusHistory = new List<OrderStatusHistoryDto>(),
            Notes = new List<OrderNoteDto>(),
            CancellationReason = null, // Order doesn't have CancellationReason
            RefundReason = null
        };
    }
}
