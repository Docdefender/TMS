using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class Ticket
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Subject { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public DateTime? SlaDueAt { get; set; }
    public DateTime? SlaBreachedAt { get; set; }
    public string RequesterUserId { get; set; } = string.Empty;
    public ApplicationUser Requester { get; set; } = null!;
    public int? RequesterDepartmentId { get; set; }
    public int? SupportDepartmentId { get; set; }
    public Department? SupportDepartment { get; set; }
    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public int? LinkedTaskId { get; set; }
    public TaskItem? LinkedTask { get; set; }
    public bool HasNewReply { get; set; }
    public bool ResolvedByLinkedTask { get; set; }
    [StringLength(500)] public string? ExternalConversationId { get; set; }
    [StringLength(320)] public string? SourceMailboxAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TicketViewer> Viewers { get; set; } = new List<TicketViewer>();
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<TicketEvent> Events { get; set; } = new List<TicketEvent>();
}
