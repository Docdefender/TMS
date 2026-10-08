using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class PipelineCheckpoint
{
    public int Id { get; set; }
    public int PipelineStageId { get; set; }
    public PipelineStage PipelineStage { get; set; } = null!;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
    public bool RequiresApproval { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<ProjectRelation> BlockingProjectRelations { get; set; } = new List<ProjectRelation>();
}
