using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class TaskItem
{
    public string? FirstAssignedByUserId { get; set; }
    public Guid? DeletionBatchId { get; set; }
    public ICollection<TaskAssignment> AssignmentHistory { get; set; } = new List<TaskAssignment>();
    public ICollection<TaskHistoryAccess> HistoryAccess { get; set; } = new List<TaskHistoryAccess>();
    public ICollection<TaskDependency> PrerequisiteDependencies { get; set; } = new List<TaskDependency>();
    public ICollection<TaskDependency> DependentDependencies { get; set; } = new List<TaskDependency>();
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? PlannedStartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ActualStartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int? PipelineStageId { get; set; }

    public PipelineStage? PipelineStage { get; set; }

    public int? PipelineCheckpointId { get; set; }

    public PipelineCheckpoint? PipelineCheckpoint { get; set; }

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? AssignedToUserId { get; set; }

    public ApplicationUser? AssignedToUser { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<TaskItemLabel> Labels { get; set; } = new List<TaskItemLabel>();

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }
}
