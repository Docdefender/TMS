using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? Details { get; set; }
}
