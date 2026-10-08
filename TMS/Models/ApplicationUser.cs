using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public int? DepartmentId { get; set; }

    [StringLength(260)]
    public string? ProfileImagePath { get; set; }

    public Department? Department { get; set; }
}
