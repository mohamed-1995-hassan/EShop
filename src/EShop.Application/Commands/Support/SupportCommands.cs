using EShop.Domain.Aggregates.SupportAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Commands.Support;

public record CreateTicketCommand(Guid CustomerId, string CustomerEmail, string CustomerName, string Subject, string Description, TicketCategory Category, TicketChannel Channel = TicketChannel.Web, Guid? OrderId = null) : IRequest<Guid>;
public record AddCustomerMessageCommand(Guid TicketId, string Content) : IRequest<Guid>;
public record AddAgentResponseCommand(Guid TicketId, Guid AgentId, string AgentName, string Content) : IRequest<Guid>;
public record AssignTicketCommand(Guid TicketId, Guid AgentId, string AgentName) : IRequest;
public record EscalateTicketCommand(Guid TicketId, TicketPriority NewPriority, string Reason) : IRequest;
public record ResolveTicketCommand(Guid TicketId, string Resolution) : IRequest;
public record CloseTicketCommand(Guid TicketId) : IRequest;
public record ReopenTicketCommand(Guid TicketId, string Reason) : IRequest;
public record RateSatisfactionCommand(Guid TicketId, int Rating, string? Feedback = null) : IRequest;

public class CreateTicketHandler : IRequestHandler<CreateTicketCommand, Guid>
{
    private readonly ISupportTicketRepository _repo;
    public CreateTicketHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        var ticket = SupportTicket.Create(request.CustomerId, request.CustomerEmail, request.CustomerName, request.Subject, request.Description, request.Category, request.Channel, request.OrderId);
        await _repo.AddAsync(ticket, ct);
        return ticket.Id;
    }
}

public class AddCustomerMessageHandler : IRequestHandler<AddCustomerMessageCommand, Guid>
{
    private readonly ISupportTicketRepository _repo;
    public AddCustomerMessageHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddCustomerMessageCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        var message = ticket.AddCustomerMessage(request.Content);
        await _repo.UpdateAsync(ticket, ct);
        return message.Id;
    }
}

public class AddAgentResponseHandler : IRequestHandler<AddAgentResponseCommand, Guid>
{
    private readonly ISupportTicketRepository _repo;
    public AddAgentResponseHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddAgentResponseCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        var message = ticket.AddAgentResponse(request.AgentId, request.AgentName, request.Content);
        await _repo.UpdateAsync(ticket, ct);
        return message.Id;
    }
}

public class AssignTicketHandler : IRequestHandler<AssignTicketCommand>
{
    private readonly ISupportTicketRepository _repo;
    public AssignTicketHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task Handle(AssignTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.AssignTo(request.AgentId, request.AgentName);
        await _repo.UpdateAsync(ticket, ct);
    }
}

public class EscalateTicketHandler : IRequestHandler<EscalateTicketCommand>
{
    private readonly ISupportTicketRepository _repo;
    public EscalateTicketHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task Handle(EscalateTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.Escalate(request.NewPriority, request.Reason);
        await _repo.UpdateAsync(ticket, ct);
    }
}

public class ResolveTicketHandler : IRequestHandler<ResolveTicketCommand>
{
    private readonly ISupportTicketRepository _repo;
    public ResolveTicketHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task Handle(ResolveTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.Resolve(request.Resolution);
        await _repo.UpdateAsync(ticket, ct);
    }
}

public class CloseTicketHandler : IRequestHandler<CloseTicketCommand>
{
    private readonly ISupportTicketRepository _repo;
    public CloseTicketHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task Handle(CloseTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.Close();
        await _repo.UpdateAsync(ticket, ct);
    }
}

public class ReopenTicketHandler : IRequestHandler<ReopenTicketCommand>
{
    private readonly ISupportTicketRepository _repo;
    public ReopenTicketHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task Handle(ReopenTicketCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.Reopen(request.Reason);
        await _repo.UpdateAsync(ticket, ct);
    }
}

public class RateSatisfactionHandler : IRequestHandler<RateSatisfactionCommand>
{
    private readonly ISupportTicketRepository _repo;
    public RateSatisfactionHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task Handle(RateSatisfactionCommand request, CancellationToken ct)
    {
        var ticket = await _repo.GetByIdAsync(request.TicketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.RateSatisfaction(request.Rating, request.Feedback);
        await _repo.UpdateAsync(ticket, ct);
    }
}
