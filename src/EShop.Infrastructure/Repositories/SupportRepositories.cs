using EShop.Domain.Aggregates.SupportAggregate;
using EShop.Domain.Repositories;
using System.Collections.Concurrent;

namespace EShop.Infrastructure.Repositories;

public class SupportTicketRepository : ISupportTicketRepository
{
    private static readonly ConcurrentDictionary<Guid, SupportTicket> _tickets = new();

    public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_tickets.GetValueOrDefault(id));

    public Task<SupportTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_tickets.Values.FirstOrDefault(t => t.TicketNumber == ticketNumber));

    public Task<IReadOnlyList<SupportTicket>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.CustomerId == customerId).OrderByDescending(t => t.CreatedAt).ToList());

    public Task<IReadOnlyList<SupportTicket>> GetByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.AssignedAgentId == agentId).OrderByDescending(t => t.UpdatedAt).ToList());

    public Task<IReadOnlyList<SupportTicket>> GetByStatusAsync(TicketStatus status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.Status == status).ToList());

    public Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.IsOpen).OrderBy(t => t.Priority).ThenBy(t => t.CreatedAt).ToList());

    public Task<IReadOnlyList<SupportTicket>> GetUnassignedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.IsOpen && !t.AssignedAgentId.HasValue).OrderBy(t => t.Priority).ThenBy(t => t.CreatedAt).ToList());

    public Task<IReadOnlyList<SupportTicket>> GetSlaBreachedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.IsSlaBreached && t.IsOpen).ToList());

    public Task<IReadOnlyList<SupportTicket>> GetByPriorityAsync(TicketPriority priority, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.Priority == priority).ToList());

    public Task<IReadOnlyList<SupportTicket>> GetByCategoryAsync(TicketCategory category, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SupportTicket>>(_tickets.Values.Where(t => t.Category == category).ToList());

    public Task<int> GetOpenCountByAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
        => Task.FromResult(_tickets.Values.Count(t => t.AssignedAgentId == agentId && t.IsOpen));

    public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _tickets[ticket.Id] = ticket;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _tickets[ticket.Id] = ticket;
        return Task.CompletedTask;
    }
}
