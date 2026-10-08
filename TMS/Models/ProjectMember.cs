namespace TMS.Models;

public class ProjectMember
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
