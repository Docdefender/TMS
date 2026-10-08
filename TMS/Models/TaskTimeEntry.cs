using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class TaskTimeEntry
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public int? DurationMinutes { get; set; }
    [StringLength(300)] public string? Note { get; set; }
    public bool IsManual { get; set; }
}
