using EShop.Domain.Aggregates.CouponAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Coupons;

/// <summary>Full coupon details</summary>
public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int CurrentUsageCount { get; set; }
    public int RemainingUsages { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Coupon summary for lists</summary>
public class CouponSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RemainingUsages { get; set; }
    public bool IsActive { get; set; }
}

public record GetCouponQuery(Guid Id) : IRequest<CouponDto?>;
public record GetCouponByCodeQuery(string Code) : IRequest<CouponDto?>;
public record GetActiveCouponsQuery : IRequest<IReadOnlyList<CouponSummaryDto>>;
public record GetExpiringSoonCouponsQuery(int WithinDays = 7) : IRequest<IReadOnlyList<CouponSummaryDto>>;
public record ValidateCouponQuery(string Code, Guid CustomerId, decimal OrderTotal, string Currency, bool IsFirstOrder) : IRequest<CouponValidationResult>;

public class GetCouponHandler : IRequestHandler<GetCouponQuery, CouponDto?>
{
    private readonly ICouponRepository _repo;
    public GetCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task<CouponDto?> Handle(GetCouponQuery request, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(request.Id, ct);
        if (c == null) return null;
        
        return new CouponDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            Type = c.Type.ToString(),
            Status = c.Status.ToString(),
            DiscountType = c.DiscountType.ToString(),
            DiscountValue = c.DiscountValue,
            MaxDiscountAmount = c.MaxDiscountAmount?.Amount,
            MinimumOrderAmount = c.MinimumOrderAmount?.Amount,
            MaxUsageCount = c.MaxUsageCount,
            CurrentUsageCount = c.CurrentUsageCount,
            RemainingUsages = c.RemainingUsages,
            ValidFrom = c.ValidFrom,
            ValidUntil = c.ValidUntil,
            IsActive = c.IsActive
        };
    }
}

public class GetCouponByCodeHandler : IRequestHandler<GetCouponByCodeQuery, CouponDto?>
{
    private readonly ICouponRepository _repo;
    public GetCouponByCodeHandler(ICouponRepository repo) => _repo = repo;

    public async Task<CouponDto?> Handle(GetCouponByCodeQuery request, CancellationToken ct)
    {
        var c = await _repo.GetByCodeAsync(request.Code, ct);
        if (c == null) return null;
        
        return new CouponDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            Type = c.Type.ToString(),
            Status = c.Status.ToString(),
            DiscountType = c.DiscountType.ToString(),
            DiscountValue = c.DiscountValue,
            MaxDiscountAmount = c.MaxDiscountAmount?.Amount,
            MinimumOrderAmount = c.MinimumOrderAmount?.Amount,
            MaxUsageCount = c.MaxUsageCount,
            CurrentUsageCount = c.CurrentUsageCount,
            RemainingUsages = c.RemainingUsages,
            ValidFrom = c.ValidFrom,
            ValidUntil = c.ValidUntil,
            IsActive = c.IsActive
        };
    }
}

public class GetActiveCouponsHandler : IRequestHandler<GetActiveCouponsQuery, IReadOnlyList<CouponSummaryDto>>
{
    private readonly ICouponRepository _repo;
    public GetActiveCouponsHandler(ICouponRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CouponSummaryDto>> Handle(GetActiveCouponsQuery request, CancellationToken ct)
    {
        var coupons = await _repo.GetActiveAsync(ct);
        return coupons.Select(c => new CouponSummaryDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            DiscountType = c.DiscountType.ToString(),
            DiscountValue = c.DiscountValue,
            Status = c.Status.ToString(),
            RemainingUsages = c.RemainingUsages,
            IsActive = c.IsActive
        }).ToList();
    }
}

public class GetExpiringSoonCouponsHandler : IRequestHandler<GetExpiringSoonCouponsQuery, IReadOnlyList<CouponSummaryDto>>
{
    private readonly ICouponRepository _repo;
    public GetExpiringSoonCouponsHandler(ICouponRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CouponSummaryDto>> Handle(GetExpiringSoonCouponsQuery request, CancellationToken ct)
    {
        var coupons = await _repo.GetExpiringSoonAsync(request.WithinDays, ct);
        return coupons.Select(c => new CouponSummaryDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            DiscountType = c.DiscountType.ToString(),
            DiscountValue = c.DiscountValue,
            Status = c.Status.ToString(),
            RemainingUsages = c.RemainingUsages,
            IsActive = c.IsActive
        }).ToList();
    }
}

public class ValidateCouponHandler : IRequestHandler<ValidateCouponQuery, CouponValidationResult>
{
    private readonly ICouponRepository _repo;
    public ValidateCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task<CouponValidationResult> Handle(ValidateCouponQuery request, CancellationToken ct)
    {
        var coupon = await _repo.GetByCodeAsync(request.Code, ct);
        if (coupon == null) return CouponValidationResult.Invalid("Coupon not found");

        return coupon.Validate(request.CustomerId, new Domain.ValueObjects.Money(request.OrderTotal, request.Currency), 
            Enumerable.Empty<Guid>(), Enumerable.Empty<Guid>(), request.IsFirstOrder);
    }
}
