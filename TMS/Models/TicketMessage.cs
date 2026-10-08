using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class TicketMessage
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string? AuthorUserId { get; set; }
    public ApplicationUser? AuthorUser { get; set; }
    [Required] public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public bool IsIncoming { get; set; }
    [StringLength(500)] public string? ExternalMessageId { get; set; }
    [StringLength(1000)] public string? ProviderMessageId { get; set; }
    [StringLength(500)] public string? ExternalConversationId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
