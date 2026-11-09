using EShop.Domain.Aggregates.ReturnAggregate;
using EShop.Domain.Repositories;
using EShop.Domain.ValueObjects;
using MediatR;

namespace EShop.Application.Commands.Returns;

// ============================================
// DTOs
// ============================================
public record CreateReturnItemDto(
    Guid OrderLineId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    ReturnReason Reason,
    string? ReasonDetails = null);

// ============================================
// CREATE RETURN
// ============================================
public record CreateReturnCommand(
    Guid OrderId,
    Guid CustomerId,
    ReturnType Type,
    string Reason,
    RefundMethod PreferredRefundMethod,
    string? CustomerNotes = null,
    List<CreateReturnItemDto>? Items = null) : IRequest<CreateReturnResult>;

public record CreateReturnResult(
    bool Success,
    Guid? ReturnId,
    string? ReturnNumber,
    string? ErrorMessage);

public class CreateReturnCommandHandler : IRequestHandler<CreateReturnCommand, CreateReturnResult>
{
    private readonly IReturnRepository _returnRepository;

    public CreateReturnCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<CreateReturnResult> Handle(CreateReturnCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = ReturnRequest.Create(
                request.OrderId,
                request.CustomerId,
                request.Type,
                request.Reason,
                request.PreferredRefundMethod,
                request.CustomerNotes);

            if (request.Items != null)
            {
                foreach (var item in request.Items)
                {
                    returnRequest.AddItem(
                        item.OrderLineId,
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        new Money(item.UnitPrice, "KWD"),
                        item.Reason,
                        item.ReasonDetails);
                }
            }

            await _returnRepository.AddAsync(returnRequest, cancellationToken);

            return new CreateReturnResult(true, returnRequest.Id, returnRequest.ReturnNumber, null);
        }
        catch (Exception ex)
        {
            return new CreateReturnResult(false, null, null, ex.Message);
        }
    }
}

// ============================================
// ADD RETURN ITEM
// ============================================
public record AddReturnItemCommand(
    Guid ReturnId,
    Guid OrderLineId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    ReturnReason Reason,
    string? ReasonDetails = null) : IRequest<ReturnOperationResult>;

public record ReturnOperationResult(bool Success, string? Status, string? ErrorMessage);

public class AddReturnItemCommandHandler : IRequestHandler<AddReturnItemCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public AddReturnItemCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(AddReturnItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.AddItem(
                request.OrderLineId,
                request.ProductId,
                request.ProductName,
                request.Quantity,
                new Money(request.UnitPrice, "KWD"),
                request.Reason,
                request.ReasonDetails);

            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// ADD RETURN IMAGE
// ============================================
public record AddReturnImageCommand(Guid ReturnId, string ImageUrl, string? Description = null) : IRequest<ReturnOperationResult>;

public class AddReturnImageCommandHandler : IRequestHandler<AddReturnImageCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public AddReturnImageCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(AddReturnImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.AddImage(request.ImageUrl, request.Description);
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// SUBMIT RETURN
// ============================================
public record SubmitReturnCommand(Guid ReturnId) : IRequest<ReturnOperationResult>;

public class SubmitReturnCommandHandler : IRequestHandler<SubmitReturnCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public SubmitReturnCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(SubmitReturnCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.Submit();
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// APPROVE RETURN
// ============================================
public record ApproveReturnCommand(Guid ReturnId, Guid ApprovedBy, string? Notes = null) : IRequest<ReturnOperationResult>;

public class ApproveReturnCommandHandler : IRequestHandler<ApproveReturnCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public ApproveReturnCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(ApproveReturnCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.Approve(request.ApprovedBy, request.Notes);
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// REJECT RETURN
// ============================================
public record RejectReturnCommand(Guid ReturnId, Guid RejectedBy, string Reason) : IRequest<ReturnOperationResult>;

public class RejectReturnCommandHandler : IRequestHandler<RejectReturnCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public RejectReturnCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(RejectReturnCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.Reject(request.RejectedBy, request.Reason);
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// GENERATE RETURN LABEL
// ============================================
public record GenerateReturnLabelCommand(Guid ReturnId, string TrackingNumber, string LabelUrl) : IRequest<ReturnLabelResult>;

public record ReturnLabelResult(bool Success, string? TrackingNumber, string? LabelUrl, string? ErrorMessage);

public class GenerateReturnLabelCommandHandler : IRequestHandler<GenerateReturnLabelCommand, ReturnLabelResult>
{
    private readonly IReturnRepository _returnRepository;

    public GenerateReturnLabelCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnLabelResult> Handle(GenerateReturnLabelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnLabelResult(false, null, null, "Return not found");

            returnRequest.GenerateReturnLabel(request.TrackingNumber, request.LabelUrl);
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnLabelResult(true, request.TrackingNumber, request.LabelUrl, null);
        }
        catch (Exception ex)
        {
            return new ReturnLabelResult(false, null, null, ex.Message);
        }
    }
}

// ============================================
// MARK RETURN SHIPPED
// ============================================
public record MarkReturnShippedCommand(Guid ReturnId) : IRequest<ReturnOperationResult>;

public class MarkReturnShippedCommandHandler : IRequestHandler<MarkReturnShippedCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public MarkReturnShippedCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(MarkReturnShippedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.MarkAsShipped();
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// MARK RETURN RECEIVED
// ============================================
public record MarkReturnReceivedCommand(Guid ReturnId) : IRequest<ReturnOperationResult>;

public class MarkReturnReceivedCommandHandler : IRequestHandler<MarkReturnReceivedCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public MarkReturnReceivedCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(MarkReturnReceivedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.MarkAsReceived();
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// START INSPECTION
// ============================================
public record StartInspectionCommand(Guid ReturnId) : IRequest<ReturnOperationResult>;

public class StartInspectionCommandHandler : IRequestHandler<StartInspectionCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public StartInspectionCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(StartInspectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.StartInspection();
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// RECORD INSPECTION RESULT
// ============================================
public record RecordInspectionResultCommand(
    Guid ReturnId,
    Guid ItemId,
    InspectionResult Result,
    string? Notes = null) : IRequest<ReturnOperationResult>;

public class RecordInspectionResultCommandHandler : IRequestHandler<RecordInspectionResultCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public RecordInspectionResultCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(RecordInspectionResultCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.RecordInspectionResult(request.ItemId, request.Result, request.Notes);
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// COMPLETE INSPECTION
// ============================================
public record CompleteInspectionCommand(Guid ReturnId) : IRequest<ReturnOperationResult>;

public class CompleteInspectionCommandHandler : IRequestHandler<CompleteInspectionCommand, ReturnOperationResult>
{
    private readonly IReturnRepository _returnRepository;

    public CompleteInspectionCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnOperationResult> Handle(CompleteInspectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnOperationResult(false, null, "Return not found");

            returnRequest.CompleteInspection();
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnOperationResult(true, returnRequest.Status.ToString(), null);
        }
        catch (Exception ex)
        {
            return new ReturnOperationResult(false, null, ex.Message);
        }
    }
}

// ============================================
// PROCESS RETURN REFUND
// ============================================
public record ProcessReturnRefundCommand(Guid ReturnId, Guid ProcessedBy) : IRequest<ReturnRefundResult>;

public record ReturnRefundResult(bool Success, decimal? RefundAmount, string? ErrorMessage);

public class ProcessReturnRefundCommandHandler : IRequestHandler<ProcessReturnRefundCommand, ReturnRefundResult>
{
    private readonly IReturnRepository _returnRepository;

    public ProcessReturnRefundCommandHandler(IReturnRepository returnRepository)
    {
        _returnRepository = returnRepository;
    }

    public async Task<ReturnRefundResult> Handle(ProcessReturnRefundCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _returnRepository.GetByIdAsync(request.ReturnId, cancellationToken);
            if (returnRequest == null)
                return new ReturnRefundResult(false, null, "Return not found");

            returnRequest.ProcessRefund(request.ProcessedBy);
            returnRequest.Complete();
            await _returnRepository.UpdateAsync(returnRequest, cancellationToken);

            return new ReturnRefundResult(true, returnRequest.RefundAmount.Amount, null);
        }
        catch (Exception ex)
        {
            return new ReturnRefundResult(false, null, ex.Message);
        }
    }
}
