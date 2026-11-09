using EShop.Application.Commands.Support;
using EShop.Application.Queries.Support;
using EShop.Domain.Aggregates.SupportAggregate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupportController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tickets/{id:guid}")]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(Guid id)
    {
        var ticket = await _mediator.Send(new GetTicketQuery(id));
        return ticket != null ? Ok(ticket) : NotFound();
    }

    [HttpGet("tickets/customer/{customerId:guid}")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetCustomerTickets(Guid customerId)
    {
        var tickets = await _mediator.Send(new GetCustomerTicketsQuery(customerId));
        return Ok(tickets);
    }

    [HttpGet("tickets/agent/{agentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetAgentTickets(Guid agentId)
    {
        var tickets = await _mediator.Send(new GetAgentTicketsQuery(agentId));
        return Ok(tickets);
    }

    [HttpGet("tickets/open")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetOpenTickets()
    {
        var tickets = await _mediator.Send(new GetOpenTicketsQuery());
        return Ok(tickets);
    }

    [HttpGet("tickets/unassigned")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetUnassignedTickets()
    {
        var tickets = await _mediator.Send(new GetUnassignedTicketsQuery());
        return Ok(tickets);
    }

    [HttpGet("tickets/sla-breached")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetSlaBreachedTickets()
    {
        var tickets = await _mediator.Send(new GetSlaBreachedTicketsQuery());
        return Ok(tickets);
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<Guid>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var id = await _mediator.Send(new CreateTicketCommand(
            request.CustomerId, request.CustomerEmail, request.CustomerName, 
            request.Subject, request.Description, request.Category, 
            request.Channel, request.OrderId));
        return CreatedAtAction(nameof(GetTicket), new { id }, id);
    }

    [HttpPost("tickets/{id:guid}/messages/customer")]
    public async Task<ActionResult<Guid>> AddCustomerMessage(Guid id, [FromBody] AddMessageRequest request)
    {
        var messageId = await _mediator.Send(new AddCustomerMessageCommand(id, request.Content));
        return Ok(messageId);
    }

    [HttpPost("tickets/{id:guid}/messages/agent")]
    public async Task<ActionResult<Guid>> AddAgentResponse(Guid id, [FromBody] AddAgentMessageRequest request)
    {
        var messageId = await _mediator.Send(new AddAgentResponseCommand(id, request.AgentId, request.AgentName, request.Content));
        return Ok(messageId);
    }

    [HttpPost("tickets/{id:guid}/assign")]
    public async Task<IActionResult> AssignTicket(Guid id, [FromBody] AssignTicketRequest request)
    {
        await _mediator.Send(new AssignTicketCommand(id, request.AgentId, request.AgentName));
        return NoContent();
    }

    [HttpPost("tickets/{id:guid}/escalate")]
    public async Task<IActionResult> EscalateTicket(Guid id, [FromBody] EscalateTicketRequest request)
    {
        await _mediator.Send(new EscalateTicketCommand(id, request.NewPriority, request.Reason));
        return NoContent();
    }

    [HttpPost("tickets/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveTicket(Guid id, [FromBody] ResolveTicketRequest request)
    {
        await _mediator.Send(new ResolveTicketCommand(id, request.Resolution));
        return NoContent();
    }

    [HttpPost("tickets/{id:guid}/close")]
    public async Task<IActionResult> CloseTicket(Guid id)
    {
        await _mediator.Send(new CloseTicketCommand(id));
        return NoContent();
    }

    [HttpPost("tickets/{id:guid}/reopen")]
    public async Task<IActionResult> ReopenTicket(Guid id, [FromBody] ReopenTicketRequest request)
    {
        await _mediator.Send(new ReopenTicketCommand(id, request.Reason));
        return NoContent();
    }

    [HttpPost("tickets/{id:guid}/rate")]
    public async Task<IActionResult> RateSatisfaction(Guid id, [FromBody] RateSatisfactionRequest request)
    {
        await _mediator.Send(new RateSatisfactionCommand(id, request.Rating, request.Feedback));
        return NoContent();
    }
}

public record CreateTicketRequest(
    Guid CustomerId, string CustomerEmail, string CustomerName,
    string Subject, string Description, TicketCategory Category,
    TicketChannel Channel = TicketChannel.Web, Guid? OrderId = null);
public record AddMessageRequest(string Content);
public record AddAgentMessageRequest(Guid AgentId, string AgentName, string Content);
public record AssignTicketRequest(Guid AgentId, string AgentName);
public record EscalateTicketRequest(TicketPriority NewPriority, string Reason);
public record ResolveTicketRequest(string Resolution);
public record ReopenTicketRequest(string Reason);
public record RateSatisfactionRequest(int Rating, string? Feedback = null);
