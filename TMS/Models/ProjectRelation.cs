namespace TMS.Models;

public enum ProjectRelationType
{
    Related,
    Dependency
}

public class ProjectRelation
{
    public int Id { get; set; }
    public int SourceProjectId { get; set; }
    public Project SourceProject { get; set; } = null!;
    public int TargetProjectId { get; set; }
    public Project TargetProject { get; set; } = null!;
    public ProjectRelationType Type { get; set; }
    public int? BlockingCheckpointId { get; set; }
    public PipelineCheckpoint? BlockingCheckpoint { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}
