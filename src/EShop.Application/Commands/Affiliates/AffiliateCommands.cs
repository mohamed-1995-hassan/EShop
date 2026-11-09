using EShop.Domain.Aggregates.AffiliateAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Affiliates;

public record ApplyForAffiliateCommand(Guid UserId, string Name, string Email, string? Website = null) : IRequest<Guid>;
public record ApproveAffiliateCommand(Guid AffiliateId) : IRequest;
public record RejectAffiliateCommand(Guid AffiliateId, string Reason) : IRequest;
public record SuspendAffiliateCommand(Guid AffiliateId, string Reason) : IRequest;
public record TrackReferralCommand(Guid AffiliateId, string IpAddress, string? UserAgent = null, string? LandingPage = null) : IRequest<Guid>;
public record ConvertReferralCommand(Guid AffiliateId, Guid ReferralId, Guid OrderId, Guid CustomerId) : IRequest;
public record EarnCommissionCommand(Guid AffiliateId, Guid OrderId, decimal OrderAmount, string Currency, string Description) : IRequest<Guid>;
public record RequestPayoutCommand(Guid AffiliateId, PayoutMethod Method) : IRequest<Guid>;
public record ProcessPayoutCommand(Guid AffiliateId, Guid PayoutId, string TransactionReference) : IRequest;
public record SetCustomCommissionRateCommand(Guid AffiliateId, decimal Rate) : IRequest;

public class ApplyForAffiliateHandler : IRequestHandler<ApplyForAffiliateCommand, Guid>
{
    private readonly IAffiliateRepository _repo;
    public ApplyForAffiliateHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<Guid> Handle(ApplyForAffiliateCommand request, CancellationToken ct)
    {
        var existing = await _repo.GetByUserIdAsync(request.UserId, ct);
        if (existing != null) return existing.Id;

        var affiliate = Affiliate.Apply(request.UserId, request.Name, request.Email, request.Website);
        await _repo.AddAsync(affiliate, ct);
        return affiliate.Id;
    }
}

public class ApproveAffiliateHandler : IRequestHandler<ApproveAffiliateCommand>
{
    private readonly IAffiliateRepository _repo;
    public ApproveAffiliateHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task Handle(ApproveAffiliateCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        affiliate.Approve();
        await _repo.UpdateAsync(affiliate, ct);
    }
}

public class RejectAffiliateHandler : IRequestHandler<RejectAffiliateCommand>
{
    private readonly IAffiliateRepository _repo;
    public RejectAffiliateHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task Handle(RejectAffiliateCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        affiliate.Reject(request.Reason);
        await _repo.UpdateAsync(affiliate, ct);
    }
}

public class SuspendAffiliateHandler : IRequestHandler<SuspendAffiliateCommand>
{
    private readonly IAffiliateRepository _repo;
    public SuspendAffiliateHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task Handle(SuspendAffiliateCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        affiliate.Suspend(request.Reason);
        await _repo.UpdateAsync(affiliate, ct);
    }
}

public class TrackReferralHandler : IRequestHandler<TrackReferralCommand, Guid>
{
    private readonly IAffiliateRepository _repo;
    public TrackReferralHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<Guid> Handle(TrackReferralCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        var referral = affiliate.TrackReferral(request.IpAddress, request.UserAgent, request.LandingPage);
        await _repo.UpdateAsync(affiliate, ct);
        return referral.Id;
    }
}

public class ConvertReferralHandler : IRequestHandler<ConvertReferralCommand>
{
    private readonly IAffiliateRepository _repo;
    public ConvertReferralHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task Handle(ConvertReferralCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        affiliate.ConvertReferral(request.ReferralId, request.OrderId, request.CustomerId);
        await _repo.UpdateAsync(affiliate, ct);
    }
}

public class EarnCommissionHandler : IRequestHandler<EarnCommissionCommand, Guid>
{
    private readonly IAffiliateRepository _repo;
    public EarnCommissionHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<Guid> Handle(EarnCommissionCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        var commission = affiliate.EarnCommission(request.OrderId, new Money(request.OrderAmount, request.Currency), request.Description);
        await _repo.UpdateAsync(affiliate, ct);
        return commission.Id;
    }
}

public class RequestPayoutHandler : IRequestHandler<RequestPayoutCommand, Guid>
{
    private readonly IAffiliateRepository _repo;
    public RequestPayoutHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<Guid> Handle(RequestPayoutCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        var payout = affiliate.RequestPayout(request.Method);
        await _repo.UpdateAsync(affiliate, ct);
        return payout.Id;
    }
}

public class ProcessPayoutHandler : IRequestHandler<ProcessPayoutCommand>
{
    private readonly IAffiliateRepository _repo;
    public ProcessPayoutHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task Handle(ProcessPayoutCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        affiliate.ProcessPayout(request.PayoutId, request.TransactionReference);
        await _repo.UpdateAsync(affiliate, ct);
    }
}

public class SetCustomCommissionRateHandler : IRequestHandler<SetCustomCommissionRateCommand>
{
    private readonly IAffiliateRepository _repo;
    public SetCustomCommissionRateHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task Handle(SetCustomCommissionRateCommand request, CancellationToken ct)
    {
        var affiliate = await _repo.GetByIdAsync(request.AffiliateId, ct) ?? throw new InvalidOperationException("Affiliate not found");
        affiliate.SetCustomCommissionRate(request.Rate);
        await _repo.UpdateAsync(affiliate, ct);
    }
}
