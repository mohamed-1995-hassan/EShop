using EShop.Domain.Aggregates.CustomerAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Customers;

// ============================================
// CREATE CUSTOMER
// ============================================
public record CreateCustomerCommand(
    string Email,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? Phone = null,
    Guid? ReferredByCustomerId = null) : IRequest<CreateCustomerResult>;

public record CreateCustomerResult(
    bool Success,
    Guid? CustomerId,
    string? ErrorMessage);

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResult>
{
    private readonly ICustomerProfileRepository _customerRepository;

    public CreateCustomerCommandHandler(ICustomerProfileRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CreateCustomerResult> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check email uniqueness
            if (await _customerRepository.EmailExistsAsync(request.Email, cancellationToken))
                return new CreateCustomerResult(false, null, "Email already registered");

            var customer = CustomerProfile.Create(
                request.Email,
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.Phone,
                request.ReferredByCustomerId);

            await _customerRepository.AddAsync(customer, cancellationToken);

            // Process referral bonus if applicable
            if (request.ReferredByCustomerId.HasValue)
            {
                var referrer = await _customerRepository.GetByIdAsync(request.ReferredByCustomerId.Value, cancellationToken);
                // Referral bonus will be processed when first order is placed
            }

            return new CreateCustomerResult(true, customer.Id, null);
        }
        catch (Exception ex)
        {
            return new CreateCustomerResult(false, null, ex.Message);
        }
    }
}

// ============================================
// ADD ADDRESS
// ============================================
public record AddCustomerAddressCommand(
    Guid CustomerId,
    string Label,
    string Street,
    string City,
    string Country,
    string PostalCode,
    bool IsDefault = false) : IRequest<AddAddressResult>;

public record AddAddressResult(bool Success, Guid? AddressId, string? ErrorMessage);

public class AddCustomerAddressCommandHandler : IRequestHandler<AddCustomerAddressCommand, AddAddressResult>
{
    private readonly ICustomerProfileRepository _customerRepository;

    public AddCustomerAddressCommandHandler(ICustomerProfileRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<AddAddressResult> Handle(AddCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetWithAddressesAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return new AddAddressResult(false, null, "Customer not found");

            var address = new Address(request.Street, request.City, request.Country, request.PostalCode);
            var customerAddress = customer.AddAddress(request.Label, address, request.IsDefault);

            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return new AddAddressResult(true, customerAddress.Id, null);
        }
        catch (Exception ex)
        {
            return new AddAddressResult(false, null, ex.Message);
        }
    }
}

// ============================================
// CREDIT WALLET
// ============================================
public record CreditWalletCommand(
    Guid CustomerId,
    decimal Amount,
    string Description,
    WalletTransactionType Type) : IRequest<WalletOperationResult>;

public record WalletOperationResult(bool Success, decimal NewBalance, string? ErrorMessage);

public class CreditWalletCommandHandler : IRequestHandler<CreditWalletCommand, WalletOperationResult>
{
    private readonly ICustomerProfileRepository _customerRepository;

    public CreditWalletCommandHandler(ICustomerProfileRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<WalletOperationResult> Handle(CreditWalletCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetWithWalletAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return new WalletOperationResult(false, 0, "Customer not found");

            customer.CreditWallet(request.Amount, request.Description, request.Type);
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return new WalletOperationResult(true, customer.WalletBalance, null);
        }
        catch (Exception ex)
        {
            return new WalletOperationResult(false, 0, ex.Message);
        }
    }
}

// ============================================
// REDEEM LOYALTY POINTS
// ============================================
public record RedeemLoyaltyPointsCommand(
    Guid CustomerId,
    int Points,
    string Description,
    Guid? OrderId = null) : IRequest<RedeemPointsResult>;

public record RedeemPointsResult(bool Success, int RemainingPoints, decimal ValueRedeemed, string? ErrorMessage);

public class RedeemLoyaltyPointsCommandHandler : IRequestHandler<RedeemLoyaltyPointsCommand, RedeemPointsResult>
{
    private readonly ICustomerProfileRepository _customerRepository;

    public RedeemLoyaltyPointsCommandHandler(ICustomerProfileRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<RedeemPointsResult> Handle(RedeemLoyaltyPointsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetWithLoyaltyAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return new RedeemPointsResult(false, 0, 0, "Customer not found");

            var valueRedeemed = customer.GetPointsValue(request.Points);
            customer.RedeemLoyaltyPoints(request.Points, request.Description, request.OrderId);
            
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return new RedeemPointsResult(true, customer.LoyaltyPoints, valueRedeemed, null);
        }
        catch (Exception ex)
        {
            return new RedeemPointsResult(false, 0, 0, ex.Message);
        }
    }
}
