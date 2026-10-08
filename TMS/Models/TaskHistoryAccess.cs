namespace TMS.Models;

public class TaskHistoryAccess
{
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByUserId { get; set; }
}
