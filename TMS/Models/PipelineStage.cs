using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class PipelineStage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
    public ICollection<PipelineCheckpoint> Checkpoints { get; set; } = new List<PipelineCheckpoint>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
