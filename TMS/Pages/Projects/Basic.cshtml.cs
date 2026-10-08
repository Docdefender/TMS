using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Services;

namespace TMS.Pages.Projects;

[Authorize]
public class BasicModel(ApplicationDbContext db, AccessService access) : PageModel
{
    public record ProjectSummary(string Name, string? Description, string? Department, string? Manager, string Status);
    public ProjectSummary Summary { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if ((await access.ProjectAsync(id)).View) return RedirectToPage("Details", new { id });
        if (!await (await access.TasksAsync()).AnyAsync(x => x.ProjectId == id)) return NotFound();
        var summary = await db.Projects.Where(x => x.Id == id).Select(x => new ProjectSummary(x.Name,
            x.Description, x.Department == null ? null : x.Department.Name,
            x.Manager == null ? null : x.Manager.FullName, x.Status.ToString())).SingleOrDefaultAsync();
        if (summary is null) return NotFound();
        Summary = summary;
        return Page();
    }
}
