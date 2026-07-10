using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class TaskItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? DueDate { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? AssignedToUserId { get; set; }

    public ApplicationUser? AssignedToUser { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }
}
