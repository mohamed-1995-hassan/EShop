using EShop.Domain.Aggregates.CustomerAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Customers;

// ============================================
// GET CUSTOMER PROFILE
// ============================================
public record GetCustomerProfileQuery(Guid CustomerId) : IRequest<CustomerProfileDto?>;

public record CustomerProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? Phone,
    DateTime DateOfBirth,
    string Tier,
    int LoyaltyPoints,
    decimal WalletBalance,
    decimal TotalLifetimeSpend,
    int TotalOrders,
    DateTime CreatedAt,
    DateTime? LastOrderAt,
    List<CustomerAddressDto> Addresses);

public record CustomerAddressDto(
    Guid Id,
    string Label,
    string Street,
    string City,
    string Country,
    string PostalCode,
    bool IsDefault);

public class GetCustomerProfileQueryHandler : IRequestHandler<GetCustomerProfileQuery, CustomerProfileDto?>
{
    private readonly ICustomerProfileRepository _customerRepository;

    public GetCustomerProfileQueryHandler(ICustomerProfileRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerProfileDto?> Handle(GetCustomerProfileQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetFullProfileAsync(request.CustomerId, cancellationToken);
        if (customer == null) return null;

        return new CustomerProfileDto(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.FullName,
            customer.Phone,
            customer.DateOfBirth,
            customer.Tier.ToString(),
            customer.LoyaltyPoints,
            customer.WalletBalance,
            customer.TotalLifetimeSpend,
            customer.TotalOrders,
            customer.CreatedAt,
            customer.LastOrderAt,
            customer.Addresses.Select(a => new CustomerAddressDto(
                a.Id,
                a.Label,
                a.Address.Street,
                a.Address.City,
                a.Address.Country,
                a.Address.PostalCode,
                a.IsDefault)).ToList());
    }
}

// ============================================
// GET LOYALTY SUMMARY
// ============================================
public record GetLoyaltySummaryQuery(Guid CustomerId) : IRequest<LoyaltySummaryDto?>;

public record LoyaltySummaryDto(
    string Tier,
    int CurrentPoints,
    decimal PointsValue,
    decimal CurrentYearSpend,
    decimal SpendToNextTier,
    string NextTier,
    List<LoyaltyTransactionDto> RecentTransactions);

public record LoyaltyTransactionDto(
    int Points,
    string Description,
    string Type,
    DateTime Date,
    DateTime? ExpiresAt);

public class GetLoyaltySummaryQueryHandler : IRequestHandler<GetLoyaltySummaryQuery, LoyaltySummaryDto?>
{
    private readonly ICustomerProfileRepository _customerRepository;

    public GetLoyaltySummaryQueryHandler(ICustomerProfileRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<LoyaltySummaryDto?> Handle(GetLoyaltySummaryQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetWithLoyaltyAsync(request.CustomerId, cancellationToken);
        if (customer == null) return null;

        var (nextTier, spendToNextTier) = CalculateNextTier(customer);

        return new LoyaltySummaryDto(
            customer.Tier.ToString(),
            customer.LoyaltyPoints,
            customer.GetPointsValue(customer.LoyaltyPoints),
            customer.CurrentYearSpend,
            spendToNextTier,
            nextTier,
            customer.LoyaltyTransactions
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new LoyaltyTransactionDto(
                    t.Points,
                    t.Description,
                    t.Type.ToString(),
                    t.CreatedAt,
                    t.ExpiresAt))
                .ToList());
    }

    private static (string NextTier, decimal SpendToNextTier) CalculateNextTier(CustomerProfile customer)
    {
        return customer.Tier switch
        {
            CustomerTier.Bronze => ("Silver", 1000 - customer.CurrentYearSpend),
            CustomerTier.Silver => ("Gold", 5000 - customer.CurrentYearSpend),
            CustomerTier.Gold => ("Platinum", 10000 - customer.CurrentYearSpend),
            CustomerTier.Platinum => ("Platinum (Max)", 0),
            _ => ("Silver", 1000)
        };
    }
}

// ============================================
// GET WALLET TRANSACTIONS
// ============================================
public record GetWalletTransactionsQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 20) : IRequest<WalletTransactionsResult?>;

public record WalletTransactionsResult(
    decimal CurrentBalance,
    List<WalletTransactionDto> Transactions,
    int TotalCount,
    int Page,
    int PageSize);

public record WalletTransactionDto(
    Guid Id,
    decimal Amount,
    string Description,
    string Type,
    DateTime Date);

public class GetWalletTransactionsQueryHandler : IRequestHandler<GetWalletTransactionsQuery, WalletTransactionsResult?>
{
    private readonly ICustomerProfileRepository _customerRepository;

    public GetWalletTransactionsQueryHandler(ICustomerProfileRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<WalletTransactionsResult?> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetWithWalletAsync(request.CustomerId, cancellationToken);
        if (customer == null) return null;

        var transactions = customer.WalletTransactions
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new WalletTransactionDto(
                t.Id,
                t.Amount,
                t.Description,
                t.Type.ToString(),
                t.CreatedAt))
            .ToList();

        return new WalletTransactionsResult(
            customer.WalletBalance,
            transactions,
            customer.WalletTransactions.Count,
            request.Page,
            request.PageSize);
    }
}
