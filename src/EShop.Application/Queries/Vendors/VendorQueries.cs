using EShop.Domain.Aggregates.VendorAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Vendors;

// Get Vendor by ID
public record GetVendorQuery(Guid VendorId) : IRequest<VendorDto?>;

public class GetVendorQueryHandler : IRequestHandler<GetVendorQuery, VendorDto?>
{
    private readonly IVendorRepository _repository;

    public GetVendorQueryHandler(IVendorRepository repository) => _repository = repository;

    public async Task<VendorDto?> Handle(GetVendorQuery request, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetByIdAsync(request.VendorId, cancellationToken);
        return vendor != null ? VendorDto.FromVendor(vendor) : null;
    }
}

// Get Active Vendors
public record GetActiveVendorsQuery(int Skip = 0, int Take = 50) : IRequest<IReadOnlyList<VendorDto>>;

public class GetActiveVendorsQueryHandler : IRequestHandler<GetActiveVendorsQuery, IReadOnlyList<VendorDto>>
{
    private readonly IVendorRepository _repository;

    public GetActiveVendorsQueryHandler(IVendorRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<VendorDto>> Handle(GetActiveVendorsQuery request, CancellationToken cancellationToken)
    {
        var vendors = await _repository.GetActiveVendorsAsync(request.Skip, request.Take, cancellationToken);
        return vendors.Select(VendorDto.FromVendor).ToList();
    }
}

// Get Pending Approval Vendors
public record GetPendingApprovalVendorsQuery : IRequest<IReadOnlyList<VendorDto>>;

public class GetPendingApprovalVendorsQueryHandler : IRequestHandler<GetPendingApprovalVendorsQuery, IReadOnlyList<VendorDto>>
{
    private readonly IVendorRepository _repository;

    public GetPendingApprovalVendorsQueryHandler(IVendorRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<VendorDto>> Handle(GetPendingApprovalVendorsQuery request, CancellationToken cancellationToken)
    {
        var vendors = await _repository.GetPendingApprovalAsync(cancellationToken);
        return vendors.Select(VendorDto.FromVendor).ToList();
    }
}

// Get Vendors by Tier
public record GetVendorsByTierQuery(VendorTier Tier) : IRequest<IReadOnlyList<VendorDto>>;

public class GetVendorsByTierQueryHandler : IRequestHandler<GetVendorsByTierQuery, IReadOnlyList<VendorDto>>
{
    private readonly IVendorRepository _repository;

    public GetVendorsByTierQueryHandler(IVendorRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<VendorDto>> Handle(GetVendorsByTierQuery request, CancellationToken cancellationToken)
    {
        var vendors = await _repository.GetByTierAsync(request.Tier, cancellationToken);
        return vendors.Select(VendorDto.FromVendor).ToList();
    }
}

// Get Vendors by Category
public record GetVendorsByCategoryQuery(Guid CategoryId) : IRequest<IReadOnlyList<VendorDto>>;

public class GetVendorsByCategoryQueryHandler : IRequestHandler<GetVendorsByCategoryQuery, IReadOnlyList<VendorDto>>
{
    private readonly IVendorRepository _repository;

    public GetVendorsByCategoryQueryHandler(IVendorRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<VendorDto>> Handle(GetVendorsByCategoryQuery request, CancellationToken cancellationToken)
    {
        var vendors = await _repository.GetByCategoryAsync(request.CategoryId, cancellationToken);
        return vendors.Select(VendorDto.FromVendor).ToList();
    }
}

// DTOs
public record VendorDto(
    Guid Id,
    string VendorCode,
    string BusinessName,
    string LegalName,
    string? Description,
    string? LogoUrl,
    string ContactEmail,
    VendorStatus Status,
    VendorTier Tier,
    decimal DefaultCommissionRate,
    int TotalOrders,
    int TotalProductsSold,
    decimal TotalRevenue,
    string Currency,
    decimal AverageRating,
    int TotalReviews,
    decimal OrderFulfillmentRate,
    decimal OnTimeShippingRate,
    decimal ReturnRate,
    IReadOnlyList<VendorCategoryDto> Categories,
    DateTime CreatedAt,
    DateTime? ApprovedAt)
{
    public static VendorDto FromVendor(Vendor v) => new(
        v.Id,
        v.VendorCode,
        v.BusinessName,
        v.LegalName,
        v.Description,
        v.LogoUrl,
        v.ContactEmail,
        v.Status,
        v.Tier,
        v.DefaultCommissionRate,
        v.TotalOrders,
        v.TotalProductsSold,
        v.TotalRevenue.Amount,
        v.TotalRevenue.Currency,
        v.AverageRating,
        v.TotalReviews,
        v.OrderFulfillmentRate,
        v.OnTimeShippingRate,
        v.ReturnRate,
        v.Categories.Select(c => new VendorCategoryDto(
            c.Id,
            c.CategoryId,
            c.CategoryName,
            c.CommissionRate,
            c.IsApproved)).ToList(),
        v.CreatedAt,
        v.ApprovedAt);
}

public record VendorCategoryDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal CommissionRate,
    bool IsApproved);
