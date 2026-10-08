namespace TMS.Models;

public class TicketViewer
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public bool IsFollowing { get; set; } = true;
}
