namespace TMS.Models;

public record TeamPickerModel(List<Department> Departments, List<ApplicationUser> Users, List<string> SelectedIds);
