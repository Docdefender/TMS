using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    // Proje Sorumlusu
    public string? ManagerUserId { get; set; }

    public ApplicationUser? Manager { get; set; }

    // Geriye dönük uyumluluk için korunuyor (artýk UI'da kullanýlmýyor)
    public string? AssignedToUserId { get; set; }

    public ApplicationUser? AssignedToUser { get; set; }

    // Ekip üyeleri
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }
}
