namespace TMS.Models;

public class TaskAssignment
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public string? PreviousUserId { get; set; }
    public string? AssignedToUserId { get; set; }
    public string AssignedByUserId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
