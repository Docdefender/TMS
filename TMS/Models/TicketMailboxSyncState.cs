using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class TicketMailboxSyncState
{
    public int Id { get; set; }
    [Required, StringLength(320)] public string MailboxAddress { get; set; } = string.Empty;
    public string? SyncLink { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessfulSyncAt { get; set; }
    [StringLength(2000)] public string? LastError { get; set; }
    public int ConsecutiveFailures { get; set; }
}
