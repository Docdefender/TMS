using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class Attachment
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    [Required]
    public string UploadedByUserId { get; set; } = string.Empty;

    public ApplicationUser UploadedByUser { get; set; } = null!;

    public int? ProjectId { get; set; }

    public Project? Project { get; set; }

    public int? TaskItemId { get; set; }

    public TaskItem? TaskItem { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }
}
