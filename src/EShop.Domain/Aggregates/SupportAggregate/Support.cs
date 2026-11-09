using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.SupportAggregate;

/// <summary>
/// Support Ticket Aggregate - Customer support system
/// 
/// BUSINESS RULES:
/// - Tickets have priority and SLA tracking
/// - Escalation workflows
/// - Agent assignment and workload balancing
/// - Customer satisfaction tracking
/// - Canned responses for efficiency
/// </summary>
public class SupportTicket : AggregateRoot<Guid>
{
    public string TicketNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketCategory Category { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketChannel Channel { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public string? AssignedAgentName { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? ProductId { get; private set; }
    public int ResponseCount { get; private set; }
    public DateTime? FirstResponseAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime? SlaDeadline { get; private set; }
    public bool IsSlaBreached { get; private set; }
    public int? SatisfactionRating { get; private set; }
    public string? SatisfactionFeedback { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<TicketMessage> _messages = new();
    public IReadOnlyCollection<TicketMessage> Messages => _messages.AsReadOnly();

    private readonly List<TicketAttachment> _attachments = new();
    public IReadOnlyCollection<TicketAttachment> Attachments => _attachments.AsReadOnly();

    private readonly List<TicketNote> _internalNotes = new();
    public IReadOnlyCollection<TicketNote> InternalNotes => _internalNotes.AsReadOnly();

    private readonly List<string> _tags = new();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public bool IsOpen => Status != TicketStatus.Closed && Status != TicketStatus.Resolved;
    public TimeSpan? ResolutionTime => ResolvedAt.HasValue ? ResolvedAt.Value - CreatedAt : null;
    public TimeSpan? FirstResponseTime => FirstResponseAt.HasValue ? FirstResponseAt.Value - CreatedAt : null;

    private SupportTicket() { }

    public static SupportTicket Create(
        Guid customerId,
        string customerEmail,
        string customerName,
        string subject,
        string description,
        TicketCategory category,
        TicketChannel channel = TicketChannel.Web,
        Guid? orderId = null,
        Guid? productId = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Subject is required");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required");

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            TicketNumber = GenerateTicketNumber(),
            CustomerId = customerId,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            Subject = subject,
            Description = description,
            Category = category,
            Priority = DeterminePriority(category),
            Status = TicketStatus.New,
            Channel = channel,
            OrderId = orderId,
            ProductId = productId,
            ResponseCount = 0,
            IsSlaBreached = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ticket.SetSlaDeadline();
        return ticket;
    }

    #region Messages

    public TicketMessage AddCustomerMessage(string content)
    {
        var message = new TicketMessage(
            Guid.NewGuid(),
            CustomerId,
            CustomerName,
            content,
            MessageSender.Customer);

        _messages.Add(message);
        UpdatedAt = DateTime.UtcNow;

        if (Status == TicketStatus.WaitingOnCustomer)
            Status = TicketStatus.InProgress;

        return message;
    }

    public TicketMessage AddAgentResponse(Guid agentId, string agentName, string content)
    {
        var message = new TicketMessage(
            Guid.NewGuid(),
            agentId,
            agentName,
            content,
            MessageSender.Agent);

        _messages.Add(message);
        ResponseCount++;
        UpdatedAt = DateTime.UtcNow;

        if (!FirstResponseAt.HasValue)
        {
            FirstResponseAt = DateTime.UtcNow;
            CheckSla();
        }

        if (Status == TicketStatus.New)
            Status = TicketStatus.InProgress;

        return message;
    }

    public TicketMessage AddSystemMessage(string content)
    {
        var message = new TicketMessage(
            Guid.NewGuid(),
            Guid.Empty,
            "System",
            content,
            MessageSender.System);

        _messages.Add(message);
        return message;
    }

    #endregion

    #region Attachments

    public TicketAttachment AddAttachment(string fileName, string fileUrl, long fileSize, Guid uploadedBy)
    {
        var attachment = new TicketAttachment(
            Guid.NewGuid(),
            fileName,
            fileUrl,
            fileSize,
            uploadedBy);

        _attachments.Add(attachment);
        return attachment;
    }

    public void RemoveAttachment(Guid attachmentId)
    {
        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment != null)
            _attachments.Remove(attachment);
    }

    #endregion

    #region Internal Notes

    public TicketNote AddInternalNote(Guid agentId, string agentName, string content)
    {
        var note = new TicketNote(Guid.NewGuid(), agentId, agentName, content);
        _internalNotes.Add(note);
        return note;
    }

    #endregion

    #region Assignment

    public void AssignTo(Guid agentId, string agentName)
    {
        AssignedAgentId = agentId;
        AssignedAgentName = agentName;
        UpdatedAt = DateTime.UtcNow;

        if (Status == TicketStatus.New)
            Status = TicketStatus.Open;

        AddSystemMessage($"Ticket assigned to {agentName}");
    }

    public void Unassign()
    {
        var previousAgent = AssignedAgentName;
        AssignedAgentId = null;
        AssignedAgentName = null;
        UpdatedAt = DateTime.UtcNow;

        AddSystemMessage($"Ticket unassigned from {previousAgent}");
    }

    public void Reassign(Guid newAgentId, string newAgentName, string reason)
    {
        var previousAgent = AssignedAgentName;
        AssignedAgentId = newAgentId;
        AssignedAgentName = newAgentName;
        UpdatedAt = DateTime.UtcNow;

        AddSystemMessage($"Ticket reassigned from {previousAgent} to {newAgentName}. Reason: {reason}");
    }

    #endregion

    #region Status Management

    public void MarkInProgress()
    {
        Status = TicketStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkWaitingOnCustomer()
    {
        Status = TicketStatus.WaitingOnCustomer;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkOnHold(string reason)
    {
        Status = TicketStatus.OnHold;
        UpdatedAt = DateTime.UtcNow;
        AddInternalNote(AssignedAgentId ?? Guid.Empty, AssignedAgentName ?? "System", $"Put on hold: {reason}");
    }

    public void Escalate(TicketPriority newPriority, string reason)
    {
        var previousPriority = Priority;
        Priority = newPriority;
        Status = TicketStatus.Escalated;
        UpdatedAt = DateTime.UtcNow;
        SetSlaDeadline();

        AddSystemMessage($"Ticket escalated from {previousPriority} to {newPriority}. Reason: {reason}");
    }

    public void Resolve(string resolution)
    {
        Status = TicketStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddAgentResponse(
            AssignedAgentId ?? Guid.Empty,
            AssignedAgentName ?? "System",
            $"Resolution: {resolution}");
    }

    public void Reopen(string reason)
    {
        if (Status != TicketStatus.Resolved && Status != TicketStatus.Closed)
            throw new DomainException("Only resolved or closed tickets can be reopened");

        Status = TicketStatus.Open;
        ResolvedAt = null;
        ClosedAt = null;
        UpdatedAt = DateTime.UtcNow;

        AddSystemMessage($"Ticket reopened. Reason: {reason}");
    }

    public void Close()
    {
        Status = TicketStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Satisfaction

    public void RateSatisfaction(int rating, string? feedback)
    {
        if (rating < 1 || rating > 5)
            throw new DomainException("Rating must be between 1 and 5");

        SatisfactionRating = rating;
        SatisfactionFeedback = feedback;
    }

    #endregion

    #region Tags

    public void AddTag(string tag)
    {
        var normalizedTag = tag.ToLowerInvariant().Trim();
        if (!_tags.Contains(normalizedTag))
            _tags.Add(normalizedTag);
    }

    public void RemoveTag(string tag)
    {
        _tags.Remove(tag.ToLowerInvariant().Trim());
    }

    #endregion

    #region Priority & SLA

    public void SetPriority(TicketPriority priority)
    {
        Priority = priority;
        SetSlaDeadline();
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetSlaDeadline()
    {
        var hours = Priority switch
        {
            TicketPriority.Critical => 1,
            TicketPriority.High => 4,
            TicketPriority.Medium => 8,
            TicketPriority.Low => 24,
            _ => 24
        };

        SlaDeadline = CreatedAt.AddHours(hours);
    }

    private void CheckSla()
    {
        if (SlaDeadline.HasValue && FirstResponseAt > SlaDeadline)
            IsSlaBreached = true;
    }

    public void CheckSlaBreached()
    {
        if (SlaDeadline.HasValue && DateTime.UtcNow > SlaDeadline && !FirstResponseAt.HasValue)
            IsSlaBreached = true;
    }

    private static TicketPriority DeterminePriority(TicketCategory category)
    {
        return category switch
        {
            TicketCategory.Payment => TicketPriority.High,
            TicketCategory.Security => TicketPriority.Critical,
            TicketCategory.OrderIssue => TicketPriority.High,
            TicketCategory.Refund => TicketPriority.Medium,
            _ => TicketPriority.Medium
        };
    }

    #endregion

    private static string GenerateTicketNumber()
    {
        return $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

public class TicketMessage : Entity<Guid>
{
    public Guid SenderId { get; private set; }
    public string SenderName { get; private set; }
    public string Content { get; private set; }
    public MessageSender SenderType { get; private set; }
    public DateTime SentAt { get; private set; }
    public bool IsRead { get; private set; }

    internal TicketMessage(Guid id, Guid senderId, string senderName, string content, MessageSender senderType)
    {
        Id = id;
        SenderId = senderId;
        SenderName = senderName;
        Content = content;
        SenderType = senderType;
        SentAt = DateTime.UtcNow;
        IsRead = senderType != MessageSender.Customer;
    }

    private TicketMessage() { }

    public void MarkAsRead() => IsRead = true;
}

public class TicketAttachment : Entity<Guid>
{
    public string FileName { get; private set; }
    public string FileUrl { get; private set; }
    public long FileSize { get; private set; }
    public Guid UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }

    internal TicketAttachment(Guid id, string fileName, string fileUrl, long fileSize, Guid uploadedBy)
    {
        Id = id;
        FileName = fileName;
        FileUrl = fileUrl;
        FileSize = fileSize;
        UploadedBy = uploadedBy;
        UploadedAt = DateTime.UtcNow;
    }

    private TicketAttachment() { }
}

public class TicketNote : Entity<Guid>
{
    public Guid AgentId { get; private set; }
    public string AgentName { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal TicketNote(Guid id, Guid agentId, string agentName, string content)
    {
        Id = id;
        AgentId = agentId;
        AgentName = agentName;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }

    private TicketNote() { }
}

public enum TicketCategory
{
    General = 1,
    OrderIssue = 2,
    Payment = 3,
    Shipping = 4,
    Return = 5,
    Refund = 6,
    ProductQuestion = 7,
    TechnicalIssue = 8,
    AccountIssue = 9,
    Feedback = 10,
    Security = 11
}

public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum TicketStatus
{
    New = 1,
    Open = 2,
    InProgress = 3,
    WaitingOnCustomer = 4,
    OnHold = 5,
    Escalated = 6,
    Resolved = 7,
    Closed = 8
}

public enum TicketChannel
{
    Web = 1,
    Email = 2,
    Phone = 3,
    Chat = 4,
    Social = 5,
    Mobile = 6
}

public enum MessageSender
{
    Customer = 1,
    Agent = 2,
    System = 3
}
