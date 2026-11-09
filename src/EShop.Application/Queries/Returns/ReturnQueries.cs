using EShop.Domain.Aggregates.ReturnAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Returns;

// ============================================
// GET RETURN BY ID
// ============================================
public record GetReturnQuery(Guid ReturnId) : IRequest<ReturnDetailsDto?>;

public record ReturnDetailsDto(
    Guid Id,
    string ReturnNumber,
    Guid OrderId,
    Guid CustomerId,
    string Status,
    string Type,
    string Reason,
    string? CustomerNotes,
    decimal RefundAmount,
    decimal? RestockingFee,
    string PreferredRefundMethod,
    string? ReturnTrackingNumber,
    string? ReturnLabelUrl,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? ReceivedAt,
    DateTime? InspectedAt,
    DateTime? CompletedAt,
    List<ReturnItemDetailDto> Items,
    List<ReturnImageDto> Images,
    List<ReturnStatusHistoryDto> StatusHistory);

public record ReturnItemDetailDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Reason,
    string? ReasonDetails,
    string? InspectionResult,
    string? InspectionNotes);

public record ReturnImageDto(
    Guid Id,
    string ImageUrl,
    string? Description,
    DateTime UploadedAt);

public record ReturnStatusHistoryDto(
    string Status,
    string Notes,
    DateTime Timestamp);

public class GetReturnQueryHandler : IRequestHandler<GetReturnQuery, ReturnDetailsDto?>
{
    private readonly IReturnRepository _returnRepository;

    public GetReturnQueryHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnDetailsDto?> Handle(GetReturnQuery request, CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
        if (returnRequest == null) return null;

        return MapToDetailsDto(returnRequest);
    }

    private static ReturnDetailsDto MapToDetailsDto(ReturnRequest returnRequest)
    {
        return new ReturnDetailsDto(
            returnRequest.Id,
            returnRequest.ReturnNumber,
            returnRequest.OrderId,
            returnRequest.CustomerId,
            returnRequest.Status.ToString(),
            returnRequest.Type.ToString(),
            returnRequest.Reason,
            returnRequest.CustomerNotes,
            returnRequest.RefundAmount.Amount,
            returnRequest.RestockingFee?.Amount,
            returnRequest.PreferredRefundMethod.ToString(),
            returnRequest.ReturnTrackingNumber,
            returnRequest.ReturnLabelUrl,
            returnRequest.CreatedAt,
            returnRequest.ApprovedAt,
            returnRequest.ReceivedAt,
            returnRequest.InspectedAt,
            returnRequest.CompletedAt,
            returnRequest.Items.Select(i => new ReturnItemDetailDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice.Amount,
                i.Reason.ToString(),
                i.ReasonDetails,
                i.InspectionResult?.ToString(),
                i.InspectionNotes)).ToList(),
            returnRequest.Images.Select(img => new ReturnImageDto(
                img.Id,
                img.ImageUrl,
                img.Description,
                img.UploadedAt)).ToList(),
            returnRequest.StatusHistory.Select(h => new ReturnStatusHistoryDto(
                h.Status.ToString(),
                h.Notes,
                h.Timestamp)).ToList());
    }
}

// ============================================
// GET RETURN BY NUMBER
// ============================================
public record GetReturnByNumberQuery(string ReturnNumber) : IRequest<ReturnDetailsDto?>;

public class GetReturnByNumberQueryHandler : IRequestHandler<GetReturnByNumberQuery, ReturnDetailsDto?>
{
    private readonly IReturnRepository _returnRepository;

    public GetReturnByNumberQueryHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnDetailsDto?> Handle(GetReturnByNumberQuery request, CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRepository.GetByReturnNumberAsync(request.ReturnNumber, cancellationToken);
        if (returnRequest == null) return null;

        return new ReturnDetailsDto(
            returnRequest.Id,
            returnRequest.ReturnNumber,
            returnRequest.OrderId,
            returnRequest.CustomerId,
            returnRequest.Status.ToString(),
            returnRequest.Type.ToString(),
            returnRequest.Reason,
            returnRequest.CustomerNotes,
            returnRequest.RefundAmount.Amount,
            returnRequest.RestockingFee?.Amount,
            returnRequest.PreferredRefundMethod.ToString(),
            returnRequest.ReturnTrackingNumber,
            returnRequest.ReturnLabelUrl,
            returnRequest.CreatedAt,
            returnRequest.ApprovedAt,
            returnRequest.ReceivedAt,
            returnRequest.InspectedAt,
            returnRequest.CompletedAt,
            returnRequest.Items.Select(i => new ReturnItemDetailDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice.Amount,
                i.Reason.ToString(),
                i.ReasonDetails,
                i.InspectionResult?.ToString(),
                i.InspectionNotes)).ToList(),
            returnRequest.Images.Select(img => new ReturnImageDto(
                img.Id,
                img.ImageUrl,
                img.Description,
                img.UploadedAt)).ToList(),
            returnRequest.StatusHistory.Select(h => new ReturnStatusHistoryDto(
                h.Status.ToString(),
                h.Notes,
                h.Timestamp)).ToList());
    }
}

// ============================================
// GET CUSTOMER RETURNS
// ============================================
public record GetCustomerReturnsQuery(Guid CustomerId) : IRequest<List<ReturnSummaryDto>>;

public record ReturnSummaryDto(
    Guid Id,
    string ReturnNumber,
    Guid OrderId,
    string Status,
    string Type,
    int ItemCount,
    decimal RefundAmount,
    DateTime CreatedAt);

public class GetCustomerReturnsQueryHandler : IRequestHandler<GetCustomerReturnsQuery, List<ReturnSummaryDto>>
{
    private readonly IReturnRepository _returnRepository;

    public GetCustomerReturnsQueryHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<List<ReturnSummaryDto>> Handle(GetCustomerReturnsQuery request, CancellationToken cancellationToken)
    {
        var returns = await _returnRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return returns.Select(r => new ReturnSummaryDto(
            r.Id,
            r.ReturnNumber,
            r.OrderId,
            r.Status.ToString(),
            r.Type.ToString(),
            r.Items.Count,
            r.RefundAmount.Amount,
            r.CreatedAt)).ToList();
    }
}

// ============================================
// GET PENDING APPROVALS (Admin)
// ============================================
public record GetPendingApprovalsQuery() : IRequest<List<ReturnSummaryDto>>;

public class GetPendingApprovalsQueryHandler : IRequestHandler<GetPendingApprovalsQuery, List<ReturnSummaryDto>>
{
    private readonly IReturnRepository _returnRepository;

    public GetPendingApprovalsQueryHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<List<ReturnSummaryDto>> Handle(GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var returns = await _returnRepository.GetPendingApprovalsAsync(cancellationToken);

        return returns.Select(r => new ReturnSummaryDto(
            r.Id,
            r.ReturnNumber,
            r.OrderId,
            r.Status.ToString(),
            r.Type.ToString(),
            r.Items.Count,
            r.RefundAmount.Amount,
            r.CreatedAt)).ToList();
    }
}

// ============================================
// GET AWAITING INSPECTION (Admin)
// ============================================
public record GetAwaitingInspectionQuery() : IRequest<List<ReturnSummaryDto>>;

public class GetAwaitingInspectionQueryHandler : IRequestHandler<GetAwaitingInspectionQuery, List<ReturnSummaryDto>>
{
    private readonly IReturnRepository _returnRepository;

    public GetAwaitingInspectionQueryHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<List<ReturnSummaryDto>> Handle(GetAwaitingInspectionQuery request, CancellationToken cancellationToken)
    {
        var returns = await _returnRepository.GetAwaitingInspectionAsync(cancellationToken);

        return returns.Select(r => new ReturnSummaryDto(
            r.Id,
            r.ReturnNumber,
            r.OrderId,
            r.Status.ToString(),
            r.Type.ToString(),
            r.Items.Count,
            r.RefundAmount.Amount,
            r.CreatedAt)).ToList();
    }
}
