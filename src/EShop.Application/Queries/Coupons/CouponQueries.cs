using EShop.Domain.Aggregates.CouponAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Coupons;

public record CouponDto(Guid Id, string Code, string Name, string? Description, CouponType Type, CouponStatus Status, DiscountType DiscountType, decimal DiscountValue, decimal? MaxDiscountAmount, decimal? MinimumOrderAmount, int? MaxUsageCount, int CurrentUsageCount, int RemainingUsages, DateTime? ValidFrom, DateTime? ValidUntil, bool IsActive);
public record CouponSummaryDto(Guid Id, string Code, string Name, DiscountType DiscountType, decimal DiscountValue, CouponStatus Status, int RemainingUsages, bool IsActive);

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
        return c == null ? null : new CouponDto(c.Id, c.Code, c.Name, c.Description, c.Type, c.Status, c.DiscountType, c.DiscountValue, c.MaxDiscountAmount?.Amount, c.MinimumOrderAmount?.Amount, c.MaxUsageCount, c.CurrentUsageCount, c.RemainingUsages, c.ValidFrom, c.ValidUntil, c.IsActive);
    }
}

public class GetCouponByCodeHandler : IRequestHandler<GetCouponByCodeQuery, CouponDto?>
{
    private readonly ICouponRepository _repo;
    public GetCouponByCodeHandler(ICouponRepository repo) => _repo = repo;

    public async Task<CouponDto?> Handle(GetCouponByCodeQuery request, CancellationToken ct)
    {
        var c = await _repo.GetByCodeAsync(request.Code, ct);
        return c == null ? null : new CouponDto(c.Id, c.Code, c.Name, c.Description, c.Type, c.Status, c.DiscountType, c.DiscountValue, c.MaxDiscountAmount?.Amount, c.MinimumOrderAmount?.Amount, c.MaxUsageCount, c.CurrentUsageCount, c.RemainingUsages, c.ValidFrom, c.ValidUntil, c.IsActive);
    }
}

public class GetActiveCouponsHandler : IRequestHandler<GetActiveCouponsQuery, IReadOnlyList<CouponSummaryDto>>
{
    private readonly ICouponRepository _repo;
    public GetActiveCouponsHandler(ICouponRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CouponSummaryDto>> Handle(GetActiveCouponsQuery request, CancellationToken ct)
    {
        var coupons = await _repo.GetActiveAsync(ct);
        return coupons.Select(c => new CouponSummaryDto(c.Id, c.Code, c.Name, c.DiscountType, c.DiscountValue, c.Status, c.RemainingUsages, c.IsActive)).ToList();
    }
}

public class GetExpiringSoonCouponsHandler : IRequestHandler<GetExpiringSoonCouponsQuery, IReadOnlyList<CouponSummaryDto>>
{
    private readonly ICouponRepository _repo;
    public GetExpiringSoonCouponsHandler(ICouponRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CouponSummaryDto>> Handle(GetExpiringSoonCouponsQuery request, CancellationToken ct)
    {
        var coupons = await _repo.GetExpiringSoonAsync(request.WithinDays, ct);
        return coupons.Select(c => new CouponSummaryDto(c.Id, c.Code, c.Name, c.DiscountType, c.DiscountValue, c.Status, c.RemainingUsages, c.IsActive)).ToList();
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
