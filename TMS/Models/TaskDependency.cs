namespace TMS.Models;

public class TaskDependency
{
    public int Id { get; set; }

    public int DependentTaskId { get; set; }
    public TaskItem DependentTask { get; set; } = null!;

    public int PrerequisiteTaskId { get; set; }
    public TaskItem PrerequisiteTask { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}
