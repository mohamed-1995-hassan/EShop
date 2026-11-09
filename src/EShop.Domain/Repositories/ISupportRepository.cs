using EShop.Domain.Aggregates.SupportAggregate;

namespace EShop.Domain.Repositories;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetByStatusAsync(TicketStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetUnassignedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetSlaBreachedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetByPriorityAsync(TicketPriority priority, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicket>> GetByCategoryAsync(TicketCategory category, CancellationToken cancellationToken = default);
    Task<int> GetOpenCountByAgentAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
}
