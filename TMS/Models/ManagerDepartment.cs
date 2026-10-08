namespace TMS.Models;

public class ManagerDepartment
{
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public bool IsDefault { get; set; }
}
