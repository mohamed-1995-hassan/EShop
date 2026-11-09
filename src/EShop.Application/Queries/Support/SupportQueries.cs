using EShop.Domain.Aggregates.SupportAggregate;
using EShop.Domain.Repositories;
using MediatR;

namespace EShop.Application.Queries.Support;

public record TicketDto(Guid Id, string TicketNumber, Guid CustomerId, string CustomerName, string Subject, TicketCategory Category, TicketPriority Priority, TicketStatus Status, Guid? AssignedAgentId, string? AssignedAgentName, bool IsSlaBreached, int MessageCount, DateTime CreatedAt);
public record TicketDetailDto(Guid Id, string TicketNumber, Guid CustomerId, string CustomerEmail, string CustomerName, string Subject, string Description, TicketCategory Category, TicketPriority Priority, TicketStatus Status, Guid? AssignedAgentId, string? AssignedAgentName, bool IsSlaBreached, int? SatisfactionRating, DateTime CreatedAt, IReadOnlyList<MessageDto> Messages);
public record MessageDto(Guid Id, Guid SenderId, string SenderName, string Content, MessageSender SenderType, DateTime SentAt);

public record GetTicketQuery(Guid Id) : IRequest<TicketDetailDto?>;
public record GetCustomerTicketsQuery(Guid CustomerId) : IRequest<IReadOnlyList<TicketDto>>;
public record GetAgentTicketsQuery(Guid AgentId) : IRequest<IReadOnlyList<TicketDto>>;
public record GetOpenTicketsQuery : IRequest<IReadOnlyList<TicketDto>>;
public record GetUnassignedTicketsQuery : IRequest<IReadOnlyList<TicketDto>>;
public record GetSlaBreachedTicketsQuery : IRequest<IReadOnlyList<TicketDto>>;

public class GetTicketHandler : IRequestHandler<GetTicketQuery, TicketDetailDto?>
{
    private readonly ISupportTicketRepository _repo;
    public GetTicketHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<TicketDetailDto?> Handle(GetTicketQuery request, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(request.Id, ct);
        if (t == null) return null;
        return new TicketDetailDto(t.Id, t.TicketNumber, t.CustomerId, t.CustomerEmail, t.CustomerName, t.Subject, t.Description, t.Category, t.Priority, t.Status, t.AssignedAgentId, t.AssignedAgentName, t.IsSlaBreached, t.SatisfactionRating, t.CreatedAt, t.Messages.Select(m => new MessageDto(m.Id, m.SenderId, m.SenderName, m.Content, m.SenderType, m.SentAt)).ToList());
    }
}

public class GetCustomerTicketsHandler : IRequestHandler<GetCustomerTicketsQuery, IReadOnlyList<TicketDto>>
{
    private readonly ISupportTicketRepository _repo;
    public GetCustomerTicketsHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TicketDto>> Handle(GetCustomerTicketsQuery request, CancellationToken ct)
    {
        var tickets = await _repo.GetByCustomerIdAsync(request.CustomerId, ct);
        return tickets.Select(t => new TicketDto(t.Id, t.TicketNumber, t.CustomerId, t.CustomerName, t.Subject, t.Category, t.Priority, t.Status, t.AssignedAgentId, t.AssignedAgentName, t.IsSlaBreached, t.Messages.Count, t.CreatedAt)).ToList();
    }
}

public class GetAgentTicketsHandler : IRequestHandler<GetAgentTicketsQuery, IReadOnlyList<TicketDto>>
{
    private readonly ISupportTicketRepository _repo;
    public GetAgentTicketsHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TicketDto>> Handle(GetAgentTicketsQuery request, CancellationToken ct)
    {
        var tickets = await _repo.GetByAgentIdAsync(request.AgentId, ct);
        return tickets.Select(t => new TicketDto(t.Id, t.TicketNumber, t.CustomerId, t.CustomerName, t.Subject, t.Category, t.Priority, t.Status, t.AssignedAgentId, t.AssignedAgentName, t.IsSlaBreached, t.Messages.Count, t.CreatedAt)).ToList();
    }
}

public class GetOpenTicketsHandler : IRequestHandler<GetOpenTicketsQuery, IReadOnlyList<TicketDto>>
{
    private readonly ISupportTicketRepository _repo;
    public GetOpenTicketsHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TicketDto>> Handle(GetOpenTicketsQuery request, CancellationToken ct)
    {
        var tickets = await _repo.GetOpenTicketsAsync(ct);
        return tickets.Select(t => new TicketDto(t.Id, t.TicketNumber, t.CustomerId, t.CustomerName, t.Subject, t.Category, t.Priority, t.Status, t.AssignedAgentId, t.AssignedAgentName, t.IsSlaBreached, t.Messages.Count, t.CreatedAt)).ToList();
    }
}

public class GetUnassignedTicketsHandler : IRequestHandler<GetUnassignedTicketsQuery, IReadOnlyList<TicketDto>>
{
    private readonly ISupportTicketRepository _repo;
    public GetUnassignedTicketsHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TicketDto>> Handle(GetUnassignedTicketsQuery request, CancellationToken ct)
    {
        var tickets = await _repo.GetUnassignedAsync(ct);
        return tickets.Select(t => new TicketDto(t.Id, t.TicketNumber, t.CustomerId, t.CustomerName, t.Subject, t.Category, t.Priority, t.Status, t.AssignedAgentId, t.AssignedAgentName, t.IsSlaBreached, t.Messages.Count, t.CreatedAt)).ToList();
    }
}

public class GetSlaBreachedTicketsHandler : IRequestHandler<GetSlaBreachedTicketsQuery, IReadOnlyList<TicketDto>>
{
    private readonly ISupportTicketRepository _repo;
    public GetSlaBreachedTicketsHandler(ISupportTicketRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TicketDto>> Handle(GetSlaBreachedTicketsQuery request, CancellationToken ct)
    {
        var tickets = await _repo.GetSlaBreachedAsync(ct);
        return tickets.Select(t => new TicketDto(t.Id, t.TicketNumber, t.CustomerId, t.CustomerName, t.Subject, t.Category, t.Priority, t.Status, t.AssignedAgentId, t.AssignedAgentName, t.IsSlaBreached, t.Messages.Count, t.CreatedAt)).ToList();
    }
}
