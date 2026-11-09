using EShop.Domain.Aggregates.AffiliateAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Affiliates;

public record AffiliateDto(Guid Id, Guid UserId, string AffiliateCode, string Name, string Email, AffiliateStatus Status, AffiliateTier Tier, decimal CommissionRate, decimal TotalEarnings, decimal PendingEarnings, decimal PaidEarnings, int TotalReferrals, int TotalConversions, decimal ConversionRate, string Currency);
public record AffiliateSummaryDto(Guid Id, string AffiliateCode, string Name, AffiliateTier Tier, decimal TotalEarnings, string Currency, AffiliateStatus Status);

public record GetAffiliateQuery(Guid Id) : IRequest<AffiliateDto?>;
public record GetAffiliateByCodeQuery(string Code) : IRequest<AffiliateDto?>;
public record GetAffiliateByUserQuery(Guid UserId) : IRequest<AffiliateDto?>;
public record GetPendingAffiliatesQuery : IRequest<IReadOnlyList<AffiliateSummaryDto>>;
public record GetTopPerformersQuery(int Count = 10) : IRequest<IReadOnlyList<AffiliateSummaryDto>>;
public record GetPendingPayoutsQuery : IRequest<IReadOnlyList<AffiliateSummaryDto>>;

public class GetAffiliateHandler : IRequestHandler<GetAffiliateQuery, AffiliateDto?>
{
    private readonly IAffiliateRepository _repo;
    public GetAffiliateHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<AffiliateDto?> Handle(GetAffiliateQuery request, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(request.Id, ct);
        return a == null ? null : new AffiliateDto(a.Id, a.UserId, a.AffiliateCode, a.Name, a.Email, a.Status, a.Tier, a.CommissionRate, a.TotalEarnings.Amount, a.PendingEarnings.Amount, a.PaidEarnings.Amount, a.TotalReferrals, a.TotalConversions, a.ConversionRate, a.TotalEarnings.Currency);
    }
}

public class GetAffiliateByCodeHandler : IRequestHandler<GetAffiliateByCodeQuery, AffiliateDto?>
{
    private readonly IAffiliateRepository _repo;
    public GetAffiliateByCodeHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<AffiliateDto?> Handle(GetAffiliateByCodeQuery request, CancellationToken ct)
    {
        var a = await _repo.GetByCodeAsync(request.Code, ct);
        return a == null ? null : new AffiliateDto(a.Id, a.UserId, a.AffiliateCode, a.Name, a.Email, a.Status, a.Tier, a.CommissionRate, a.TotalEarnings.Amount, a.PendingEarnings.Amount, a.PaidEarnings.Amount, a.TotalReferrals, a.TotalConversions, a.ConversionRate, a.TotalEarnings.Currency);
    }
}

public class GetAffiliateByUserHandler : IRequestHandler<GetAffiliateByUserQuery, AffiliateDto?>
{
    private readonly IAffiliateRepository _repo;
    public GetAffiliateByUserHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<AffiliateDto?> Handle(GetAffiliateByUserQuery request, CancellationToken ct)
    {
        var a = await _repo.GetByUserIdAsync(request.UserId, ct);
        return a == null ? null : new AffiliateDto(a.Id, a.UserId, a.AffiliateCode, a.Name, a.Email, a.Status, a.Tier, a.CommissionRate, a.TotalEarnings.Amount, a.PendingEarnings.Amount, a.PaidEarnings.Amount, a.TotalReferrals, a.TotalConversions, a.ConversionRate, a.TotalEarnings.Currency);
    }
}

public class GetPendingAffiliatesHandler : IRequestHandler<GetPendingAffiliatesQuery, IReadOnlyList<AffiliateSummaryDto>>
{
    private readonly IAffiliateRepository _repo;
    public GetPendingAffiliatesHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AffiliateSummaryDto>> Handle(GetPendingAffiliatesQuery request, CancellationToken ct)
    {
        var affiliates = await _repo.GetByStatusAsync(AffiliateStatus.Pending, ct);
        return affiliates.Select(a => new AffiliateSummaryDto(a.Id, a.AffiliateCode, a.Name, a.Tier, a.TotalEarnings.Amount, a.TotalEarnings.Currency, a.Status)).ToList();
    }
}

public class GetTopPerformersHandler : IRequestHandler<GetTopPerformersQuery, IReadOnlyList<AffiliateSummaryDto>>
{
    private readonly IAffiliateRepository _repo;
    public GetTopPerformersHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AffiliateSummaryDto>> Handle(GetTopPerformersQuery request, CancellationToken ct)
    {
        var affiliates = await _repo.GetTopPerformersAsync(request.Count, ct);
        return affiliates.Select(a => new AffiliateSummaryDto(a.Id, a.AffiliateCode, a.Name, a.Tier, a.TotalEarnings.Amount, a.TotalEarnings.Currency, a.Status)).ToList();
    }
}

public class GetPendingPayoutsHandler : IRequestHandler<GetPendingPayoutsQuery, IReadOnlyList<AffiliateSummaryDto>>
{
    private readonly IAffiliateRepository _repo;
    public GetPendingPayoutsHandler(IAffiliateRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AffiliateSummaryDto>> Handle(GetPendingPayoutsQuery request, CancellationToken ct)
    {
        var affiliates = await _repo.GetPendingPayoutsAsync(ct);
        return affiliates.Select(a => new AffiliateSummaryDto(a.Id, a.AffiliateCode, a.Name, a.Tier, a.TotalEarnings.Amount, a.TotalEarnings.Currency, a.Status)).ToList();
    }
}
