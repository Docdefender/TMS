using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int? ProjectId { get; set; }

    public Project? Project { get; set; }

    public int? TaskItemId { get; set; }

    public TaskItem? TaskItem { get; set; }

    public TaskStatus? NewTaskStatus { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }
}
