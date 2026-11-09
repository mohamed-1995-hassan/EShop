using EShop.Application.Commands.Customers;
using EShop.Application.Queries.Customers;
using EShop.Domain.Aggregates.CustomerAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get customer profile
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerProfileDto>> GetProfile(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerProfileQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Register a new customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateCustomerResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCustomerResult>> Register(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Phone,
            request.ReferralCode);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return CreatedAtAction(nameof(GetProfile), new { id = result.CustomerId }, result);
    }

    /// <summary>
    /// Add a shipping address
    /// </summary>
    [HttpPost("{id:guid}/addresses")]
    [ProducesResponseType(typeof(AddAddressResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddAddressResult>> AddAddress(
        Guid id,
        [FromBody] AddAddressRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddCustomerAddressCommand(
            id,
            request.Label,
            request.Street,
            request.City,
            request.Country,
            request.PostalCode,
            request.IsDefault);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Created($"/api/customers/{id}/addresses/{result.AddressId}", result);
    }

    /// <summary>
    /// Get loyalty program summary
    /// </summary>
    [HttpGet("{id:guid}/loyalty")]
    [ProducesResponseType(typeof(LoyaltySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoyaltySummaryDto>> GetLoyaltySummary(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLoyaltySummaryQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Redeem loyalty points
    /// </summary>
    [HttpPost("{id:guid}/loyalty/redeem")]
    [ProducesResponseType(typeof(RedeemPointsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RedeemPointsResult>> RedeemPoints(
        Guid id,
        [FromBody] RedeemPointsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RedeemLoyaltyPointsCommand(id, request.Points, request.Description, request.OrderId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }

    /// <summary>
    /// Get wallet transactions
    /// </summary>
    [HttpGet("{id:guid}/wallet")]
    [ProducesResponseType(typeof(WalletTransactionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletTransactionsResult>> GetWalletTransactions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetWalletTransactionsQuery(id, page, pageSize), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Credit wallet (Admin)
    /// </summary>
    [HttpPost("{id:guid}/wallet/credit")]
    [ProducesResponseType(typeof(WalletOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletOperationResult>> CreditWallet(
        Guid id,
        [FromBody] CreditWalletRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreditWalletCommand(id, request.Amount, request.Description, request.Type);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Detail = result.ErrorMessage });

        return Ok(result);
    }
}

// Request DTOs
public record RegisterCustomerRequest(
    string Email,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? Phone = null,
    Guid? ReferralCode = null);

public record AddAddressRequest(
    string Label,
    string Street,
    string City,
    string Country,
    string PostalCode,
    bool IsDefault = false);

public record RedeemPointsRequest(
    int Points,
    string Description,
    Guid? OrderId = null);

public record CreditWalletRequest(
    decimal Amount,
    string Description,
    WalletTransactionType Type);
