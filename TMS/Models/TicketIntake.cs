using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

// Stores an email-like request until its sender has an application user account.
public class TicketIntake
{
    public int Id { get; set; }
    [Required, StringLength(320)] public string SenderEmail { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Subject { get; set; } = string.Empty;
    [Required] public string Body { get; set; } = string.Empty;
    [StringLength(500)] public string? ExternalMessageId { get; set; }
    [StringLength(1000)] public string? ProviderMessageId { get; set; }
    [StringLength(500)] public string? ExternalConversationId { get; set; }
    [StringLength(320)] public string? SourceMailboxAddress { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public int SupportDepartmentId { get; set; }
    public Department SupportDepartment { get; set; } = null!;
    public int? MatchedTicketId { get; set; }
    public Ticket? MatchedTicket { get; set; }
    public DateTime? MatchedAt { get; set; }
    public string? MatchedByUserId { get; set; }
}
