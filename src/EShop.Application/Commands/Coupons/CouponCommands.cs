using EShop.Domain.Aggregates.CouponAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Coupons;

public record CreatePercentageCouponCommand(string Code, string Name, decimal Percentage, decimal? MaxDiscount = null, int? MaxUsage = null) : IRequest<Guid>;
public record CreateFixedAmountCouponCommand(string Code, string Name, decimal Amount, string Currency, int? MaxUsage = null) : IRequest<Guid>;
public record CreateFreeShippingCouponCommand(string Code, string Name, int? MaxUsage = null) : IRequest<Guid>;
public record GenerateBulkCouponsCommand(string Prefix, string Name, DiscountType DiscountType, decimal DiscountValue, int Quantity, int UsagePerCoupon = 1) : IRequest<IReadOnlyList<Guid>>;
public record ApplyCouponCommand(Guid CouponId, Guid OrderId, Guid CustomerId, decimal DiscountApplied, string Currency) : IRequest<Guid>;
public record ActivateCouponCommand(Guid CouponId) : IRequest;
public record DeactivateCouponCommand(Guid CouponId) : IRequest;
public record SetCouponValidityCommand(Guid CouponId, DateTime? ValidFrom, DateTime? ValidUntil) : IRequest;
public record SetCouponMinOrderCommand(Guid CouponId, decimal MinimumAmount, string Currency) : IRequest;

public class CreatePercentageCouponHandler : IRequestHandler<CreatePercentageCouponCommand, Guid>
{
    private readonly ICouponRepository _repo;
    public CreatePercentageCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreatePercentageCouponCommand request, CancellationToken ct)
    {
        var coupon = Coupon.CreatePercentage(request.Code, request.Name, request.Percentage,
            request.MaxDiscount.HasValue ? new Money(request.MaxDiscount.Value, "USD") : null, request.MaxUsage);
        await _repo.AddAsync(coupon, ct);
        return coupon.Id;
    }
}

public class CreateFixedAmountCouponHandler : IRequestHandler<CreateFixedAmountCouponCommand, Guid>
{
    private readonly ICouponRepository _repo;
    public CreateFixedAmountCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateFixedAmountCouponCommand request, CancellationToken ct)
    {
        var coupon = Coupon.CreateFixedAmount(request.Code, request.Name, new Money(request.Amount, request.Currency), request.MaxUsage);
        await _repo.AddAsync(coupon, ct);
        return coupon.Id;
    }
}

public class CreateFreeShippingCouponHandler : IRequestHandler<CreateFreeShippingCouponCommand, Guid>
{
    private readonly ICouponRepository _repo;
    public CreateFreeShippingCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateFreeShippingCouponCommand request, CancellationToken ct)
    {
        var coupon = Coupon.CreateFreeShipping(request.Code, request.Name, request.MaxUsage);
        await _repo.AddAsync(coupon, ct);
        return coupon.Id;
    }
}

public class GenerateBulkCouponsHandler : IRequestHandler<GenerateBulkCouponsCommand, IReadOnlyList<Guid>>
{
    private readonly ICouponRepository _repo;
    public GenerateBulkCouponsHandler(ICouponRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<Guid>> Handle(GenerateBulkCouponsCommand request, CancellationToken ct)
    {
        var coupons = Coupon.GenerateBulk(request.Prefix, request.Name, request.DiscountType, request.DiscountValue, request.Quantity, request.UsagePerCoupon);
        await _repo.AddRangeAsync(coupons, ct);
        return coupons.Select(c => c.Id).ToList();
    }
}

public class ApplyCouponHandler : IRequestHandler<ApplyCouponCommand, Guid>
{
    private readonly ICouponRepository _repo;
    public ApplyCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task<Guid> Handle(ApplyCouponCommand request, CancellationToken ct)
    {
        var coupon = await _repo.GetByIdAsync(request.CouponId, ct) ?? throw new InvalidOperationException("Coupon not found");
        var usage = coupon.Apply(request.OrderId, request.CustomerId, new Money(request.DiscountApplied, request.Currency));
        await _repo.UpdateAsync(coupon, ct);
        return usage.Id;
    }
}

public class ActivateCouponHandler : IRequestHandler<ActivateCouponCommand>
{
    private readonly ICouponRepository _repo;
    public ActivateCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task Handle(ActivateCouponCommand request, CancellationToken ct)
    {
        var coupon = await _repo.GetByIdAsync(request.CouponId, ct) ?? throw new InvalidOperationException("Coupon not found");
        coupon.Activate();
        await _repo.UpdateAsync(coupon, ct);
    }
}

public class DeactivateCouponHandler : IRequestHandler<DeactivateCouponCommand>
{
    private readonly ICouponRepository _repo;
    public DeactivateCouponHandler(ICouponRepository repo) => _repo = repo;

    public async Task Handle(DeactivateCouponCommand request, CancellationToken ct)
    {
        var coupon = await _repo.GetByIdAsync(request.CouponId, ct) ?? throw new InvalidOperationException("Coupon not found");
        coupon.Deactivate();
        await _repo.UpdateAsync(coupon, ct);
    }
}

public class SetCouponValidityHandler : IRequestHandler<SetCouponValidityCommand>
{
    private readonly ICouponRepository _repo;
    public SetCouponValidityHandler(ICouponRepository repo) => _repo = repo;

    public async Task Handle(SetCouponValidityCommand request, CancellationToken ct)
    {
        var coupon = await _repo.GetByIdAsync(request.CouponId, ct) ?? throw new InvalidOperationException("Coupon not found");
        coupon.SetValidityPeriod(request.ValidFrom, request.ValidUntil);
        await _repo.UpdateAsync(coupon, ct);
    }
}

public class SetCouponMinOrderHandler : IRequestHandler<SetCouponMinOrderCommand>
{
    private readonly ICouponRepository _repo;
    public SetCouponMinOrderHandler(ICouponRepository repo) => _repo = repo;

    public async Task Handle(SetCouponMinOrderCommand request, CancellationToken ct)
    {
        var coupon = await _repo.GetByIdAsync(request.CouponId, ct) ?? throw new InvalidOperationException("Coupon not found");
        coupon.SetMinimumOrderAmount(new Money(request.MinimumAmount, request.Currency));
        await _repo.UpdateAsync(coupon, ct);
    }
}
