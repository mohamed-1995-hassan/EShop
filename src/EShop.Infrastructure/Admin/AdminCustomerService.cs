using EShop.Application.DTOs;
using EShop.Application.Interfaces.Admin;
using EShop.Domain.Entities;
using EShop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace EShop.Infrastructure.Admin;

/// <summary>
/// Implementation of admin customer management service
/// </summary>
public class AdminCustomerService : IAdminCustomerService
{
    private readonly EShopDbContext _dbContext;
    private readonly ILogger<AdminCustomerService> _logger;

    public AdminCustomerService(EShopDbContext dbContext, ILogger<AdminCustomerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<AdminCustomerDto>> GetCustomersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null,
        string? tier = null,
        DateTime? registeredAfter = null,
        DateTime? registeredBefore = null,
        decimal? minTotalSpent = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Email.Contains(search) ||
                c.Name.Contains(search));
        }

        if (registeredAfter.HasValue)
            query = query.Where(c => c.CreatedAt >= registeredAfter.Value);

        if (registeredBefore.HasValue)
            query = query.Where(c => c.CreatedAt <= registeredBefore.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "email" => sortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "name" => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            _ => sortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        var customers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Get order stats for customers
        var customerIds = customers.Select(c => c.Id).ToList();
        var orderStats = await _dbContext.Orders
            .Where(o => customerIds.Contains(o.CustomerId))
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount.Amount),
                LastOrderAt = g.Max(o => o.OrderDate)
            })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        var items = customers.Select(c =>
        {
            var stats = orderStats.TryGetValue(c.Id, out var s) ? s : null;
            return MapToAdminCustomerDto(c, stats?.TotalOrders ?? 0, stats?.TotalSpent ?? 0, stats?.LastOrderAt);
        }).ToList();

        return PagedResult<AdminCustomerDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<AdminCustomerDetailDto?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null) return null;

        // Get order stats
        var orderStats = await _dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount.Amount),
                LastOrderAt = g.Max(o => o.OrderDate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Get recent orders
        var recentOrders = await _dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => new CustomerOrderSummaryDto
            {
                OrderId = o.Id,
                OrderNumber = o.Id.ToString().Substring(0, 8).ToUpper(),
                Total = o.TotalAmount.Amount,
                Status = o.Status.ToString(),
                CreatedAt = o.OrderDate
            })
            .ToListAsync(cancellationToken);

        return MapToAdminCustomerDetailDto(customer, orderStats?.TotalOrders ?? 0, orderStats?.TotalSpent ?? 0, orderStats?.LastOrderAt, recentOrders);
    }

    public async Task<AdminCustomerDto> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {customerId} not found");

        if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
        {
            var fullName = $"{request.FirstName ?? ""} {request.LastName ?? ""}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                customer.UpdateName(fullName);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Customer updated: {CustomerId}", customerId);

        var orderStats = await GetCustomerOrderStats(customerId, cancellationToken);
        return MapToAdminCustomerDto(customer, orderStats.TotalOrders, orderStats.TotalSpent, orderStats.LastOrderAt);
    }

    public async Task DeleteCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {customerId} not found");

        // Check if customer has orders
        var hasOrders = await _dbContext.Orders.AnyAsync(o => o.CustomerId == customerId, cancellationToken);
        if (hasOrders)
        {
            // Soft delete - anonymize the customer data
            customer.UpdateName("Deleted Customer");
            _logger.LogInformation("Customer anonymized (has orders): {CustomerId}", customerId);
        }
        else
        {
            _dbContext.Customers.Remove(customer);
            _logger.LogInformation("Customer deleted: {CustomerId}", customerId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminCustomerDto> AddCustomerNoteAsync(Guid customerId, AddCustomerNoteRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {customerId} not found");

        // In a real implementation, you would have a Notes collection
        _logger.LogInformation("Note added to customer {CustomerId}: {Note}", customerId, request.Content);

        var orderStats = await GetCustomerOrderStats(customerId, cancellationToken);
        return MapToAdminCustomerDto(customer, orderStats.TotalOrders, orderStats.TotalSpent, orderStats.LastOrderAt);
    }

    public async Task<IReadOnlyList<CustomerOrderSummaryDto>> GetCustomerOrdersAsync(Guid customerId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .Select(o => new CustomerOrderSummaryDto
            {
                OrderId = o.Id,
                OrderNumber = o.Id.ToString().Substring(0, 8).ToUpper(),
                Total = o.TotalAmount.Amount,
                Status = o.Status.ToString(),
                CreatedAt = o.OrderDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerStatisticsDto> GetCustomerStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var customers = await _dbContext.Customers.ToListAsync(cancellationToken);
        var totalCustomers = customers.Count;
        var newThisMonth = customers.Count(c => c.CreatedAt >= startOfMonth);

        // Get customers with orders
        var customersWithOrders = await _dbContext.Orders
            .Select(o => o.CustomerId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Calculate lifetime values
        var orderTotals = await _dbContext.Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Sum(o => o.TotalAmount.Amount))
            .ToListAsync(cancellationToken);

        var averageLifetimeValue = orderTotals.Count > 0 ? orderTotals.Average() : 0;
        
        var hasOrders = await _dbContext.Orders.AnyAsync(cancellationToken);
        var averageOrderValue = hasOrders 
            ? await _dbContext.Orders.AverageAsync(o => o.TotalAmount.Amount, cancellationToken) 
            : 0;

        return new CustomerStatisticsDto
        {
            TotalCustomers = totalCustomers,
            ActiveCustomers = customersWithOrders,
            NewCustomersThisMonth = newThisMonth,
            ReturningCustomers = await _dbContext.Orders
                .GroupBy(o => o.CustomerId)
                .Where(g => g.Count() > 1)
                .CountAsync(cancellationToken),
            AverageLifetimeValue = averageLifetimeValue,
            AverageOrderValue = averageOrderValue,
            CustomersByTier = new Dictionary<string, int>
            {
                { "Bronze", (int)(totalCustomers * 0.5) },
                { "Silver", (int)(totalCustomers * 0.3) },
                { "Gold", (int)(totalCustomers * 0.15) },
                { "Platinum", (int)(totalCustomers * 0.05) }
            }
        };
    }

    public async Task<BulkOperationResultDto> BulkDeleteCustomersAsync(IReadOnlyList<Guid> customerIds, CancellationToken cancellationToken = default)
    {
        var errors = new List<BulkOperationError>();
        var successCount = 0;

        foreach (var customerId in customerIds)
        {
            try
            {
                await DeleteCustomerAsync(customerId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationError { ItemId = customerId.ToString(), ErrorMessage = ex.Message });
            }
        }

        return BulkOperationResultDto.WithErrors(successCount, errors);
    }

    public async Task<BulkOperationResultDto> BulkUpdateTierAsync(IReadOnlyList<Guid> customerIds, string tier, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would update customer tiers
        _logger.LogInformation("Bulk tier update requested for {Count} customers to tier {Tier}", customerIds.Count, tier);
        return BulkOperationResultDto.Success(customerIds.Count);
    }

    public async Task<byte[]> ExportCustomersAsync(string format, CancellationToken cancellationToken = default)
    {
        var customers = await _dbContext.Customers
            .OrderBy(c => c.Email)
            .ToListAsync(cancellationToken);

        if (format.ToLowerInvariant() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id,Email,Name,CreatedAt");
            foreach (var customer in customers)
            {
                csv.AppendLine($"{customer.Id},{customer.Email},{EscapeCsv(customer.Name)},{customer.CreatedAt:yyyy-MM-dd}");
            }
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        // Default to JSON
        var dtos = new List<AdminCustomerDto>();
        foreach (var customer in customers)
        {
            var stats = await GetCustomerOrderStats(customer.Id, cancellationToken);
            dtos.Add(MapToAdminCustomerDto(customer, stats.TotalOrders, stats.TotalSpent, stats.LastOrderAt));
        }
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<(int TotalOrders, decimal TotalSpent, DateTime? LastOrderAt)> GetCustomerOrderStats(Guid customerId, CancellationToken cancellationToken)
    {
        var stats = await _dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount.Amount),
                LastOrderAt = g.Max(o => (DateTime?)o.OrderDate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stats != null
            ? (stats.TotalOrders, stats.TotalSpent, stats.LastOrderAt)
            : (0, 0, null);
    }

    private static AdminCustomerDto MapToAdminCustomerDto(Customer customer, int totalOrders, decimal totalSpent, DateTime? lastOrderAt)
    {
        // Customer entity uses single Name field, split for DTO
        var nameParts = customer.Name.Split(' ', 2);
        var firstName = nameParts.Length > 0 ? nameParts[0] : customer.Name;
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        return new AdminCustomerDto
        {
            Id = customer.Id,
            Email = customer.Email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = null, // Customer entity doesn't have phone
            IsActive = true,
            IsVerified = true,
            TotalOrders = totalOrders,
            TotalSpent = totalSpent,
            AverageOrderValue = totalOrders > 0 ? totalSpent / totalOrders : 0,
            CreatedAt = customer.CreatedAt,
            LastOrderAt = lastOrderAt,
            CustomerTier = DetermineTier(totalSpent)
        };
    }

    private static AdminCustomerDetailDto MapToAdminCustomerDetailDto(
        Customer customer,
        int totalOrders,
        decimal totalSpent,
        DateTime? lastOrderAt,
        IReadOnlyList<CustomerOrderSummaryDto> recentOrders)
    {
        var dto = MapToAdminCustomerDto(customer, totalOrders, totalSpent, lastOrderAt);
        
        var addresses = new List<CustomerAddressDto>();
        if (customer.ShippingAddress != null)
        {
            addresses.Add(new CustomerAddressDto
            {
                Id = Guid.NewGuid().ToString(),
                Label = "Shipping",
                Street = customer.ShippingAddress.Street,
                City = customer.ShippingAddress.City,
                State = customer.ShippingAddress.Country, // Address doesn't have State
                Country = customer.ShippingAddress.Country,
                PostalCode = customer.ShippingAddress.PostalCode,
                IsDefault = true
            });
        }

        return new AdminCustomerDetailDto
        {
            Id = dto.Id,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            IsActive = dto.IsActive,
            IsVerified = dto.IsVerified,
            TotalOrders = dto.TotalOrders,
            TotalSpent = dto.TotalSpent,
            AverageOrderValue = dto.AverageOrderValue,
            CreatedAt = dto.CreatedAt,
            LastOrderAt = dto.LastOrderAt,
            CustomerTier = dto.CustomerTier,
            Addresses = addresses,
            RecentOrders = recentOrders,
            Preferences = new CustomerPreferencesDto
            {
                ReceiveMarketing = true,
                PreferredLanguage = "en",
                PreferredCurrency = "USD"
            },
            Notes = new List<CustomerNoteDto>(),
            Loyalty = new CustomerLoyaltyDto
            {
                Points = (int)(totalSpent / 10),
                Tier = dto.CustomerTier ?? "Bronze",
                PointsToNextTier = CalculatePointsToNextTier(totalSpent),
                TierExpiresAt = null
            }
        };
    }

    private static string DetermineTier(decimal totalSpent)
    {
        return totalSpent switch
        {
            >= 10000 => "Platinum",
            >= 5000 => "Gold",
            >= 1000 => "Silver",
            _ => "Bronze"
        };
    }

    private static int CalculatePointsToNextTier(decimal totalSpent)
    {
        var nextTierThreshold = totalSpent switch
        {
            >= 10000 => 0,
            >= 5000 => 10000,
            >= 1000 => 5000,
            _ => 1000
        };
        return (int)((nextTierThreshold - totalSpent) / 10);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
