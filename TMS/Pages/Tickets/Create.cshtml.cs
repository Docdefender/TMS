using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Services;
using TMS.Models;

namespace TMS.Pages.Tickets;

[Authorize]
public class CreateModel(ApplicationDbContext db, TicketService tickets, AccessService access) : PageModel
{
    [BindProperty, Required, StringLength(200)] public string Subject { get; set; } = string.Empty;
    [BindProperty, Required] public string Body { get; set; } = string.Empty;
    [BindProperty, Required] public string RequesterUserId { get; set; } = string.Empty;
    [BindProperty] public int SupportDepartmentId { get; set; }
    [BindProperty] public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public List<SelectListItem> Users { get; private set; } = [];
    public List<SelectListItem> Departments { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadOptionsAsync()) return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await LoadOptionsAsync()) return Forbid();
        if (!ModelState.IsValid) return Page();
        try
        {
            var ticket = await tickets.CreateAsync(Subject, RequesterUserId, SupportDepartmentId, Body, Priority);
            return RedirectToPage("/Tickets/Details", new { id = ticket.Id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private async Task<bool> LoadOptionsAsync()
    {
        var actor = await access.ActorAsync();
        var ownDepartment = await db.Users.Where(u => u.Id == actor.UserId)
            .Select(u => u.DepartmentId).SingleOrDefaultAsync();
        if (!actor.IsAdmin && (!ownDepartment.HasValue || !await db.Departments.AnyAsync(d => d.Id == ownDepartment
            && d.IsTicketSupport && !d.IsDeleted)))
            return false;
        var departments = db.Departments.AsNoTracking().Where(d => !d.IsDeleted && d.IsTicketSupport);
        if (!actor.IsAdmin) departments = departments.Where(d => d.Id == ownDepartment);
        Departments = await departments.OrderBy(d => d.Name).Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToListAsync();
        if (SupportDepartmentId == 0 && Departments.Count == 1
            && int.TryParse(Departments[0].Value, out var defaultDepartmentId))
            SupportDepartmentId = defaultDepartmentId;
        Users = await db.Users.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.FullName)
            .Select(u => new SelectListItem(u.FullName + " (" + u.Email + ")", u.Id)).ToListAsync();
        return true;
    }
}
