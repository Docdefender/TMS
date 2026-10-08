using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class Notification
{
    public int Id { get; set; }
    [Required, StringLength(450)] public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    [StringLength(450)] public string? ActorUserId { get; set; }
    public ApplicationUser? ActorUser { get; set; }
    [Required, StringLength(40)] public string Type { get; set; } = "Info";
    [Required, StringLength(140)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(400)] public string Message { get; set; } = string.Empty;
    [StringLength(500)] public string? Url { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
