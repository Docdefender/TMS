using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class TicketEvent
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string? ActorUserId { get; set; }
    public ApplicationUser? ActorUser { get; set; }
    [Required, StringLength(40)] public string Type { get; set; } = string.Empty;
    [StringLength(1000)] public string? Details { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
