using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Tickets;

[Authorize]
public class UnmatchedModel(ApplicationDbContext db, AccessService access, TicketIntakeService intakes,
    IConfiguration configuration) : PageModel
{
    private readonly bool _mailIntakeEnabled = configuration.GetValue<bool>($"{TicketMailOptions.SectionName}:Enabled");
    [BindProperty, Required, EmailAddress] public string SenderEmail { get; set; } = string.Empty;
    [BindProperty, Required, StringLength(200)] public string Subject { get; set; } = string.Empty;
    [BindProperty, Required] public string Body { get; set; } = string.Empty;
    [BindProperty] public int SupportDepartmentId { get; set; }
    [BindProperty] public DateTime? ReceivedAt { get; set; }
    public List<TicketIntake> Items { get; private set; } = [];
    public List<SelectListItem> Departments { get; private set; } = [];
    public Dictionary<int, List<SelectListItem>> MatchingUsers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_mailIntakeEnabled) return NotFound();
        if (!await LoadAsync()) return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostStageAsync()
    {
        if (!_mailIntakeEnabled) return NotFound();
        if (!await LoadAsync()) return Forbid();
        if (!ModelState.IsValid) return Page();
        try
        {
            var time = ReceivedAt.HasValue ? DateTime.SpecifyKind(ReceivedAt.Value, DateTimeKind.Local).ToUniversalTime() : (DateTime?)null;
            await intakes.StageAsync(SenderEmail, Subject, Body, SupportDepartmentId, time);
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostMatchAsync(int id, string requesterUserId)
    {
        if (!_mailIntakeEnabled) return NotFound();
        if (!await LoadAsync()) return Forbid();
        try
        {
            var ticketId = await intakes.MatchAsync(id, requesterUserId);
            return RedirectToPage("/Tickets/Details", new { id = ticketId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task<bool> LoadAsync()
    {
        var actor = await access.ActorAsync();
        var canWork = actor.IsAdmin || actor.HomeDepartmentId.HasValue
            && await db.Departments.AnyAsync(d => d.Id == actor.HomeDepartmentId
                && d.IsTicketSupport && !d.IsDeleted);
        if (!canWork) return false;
        var departments = db.Departments.AsNoTracking().Where(d => !d.IsDeleted && d.IsTicketSupport);
        if (!actor.IsAdmin) departments = departments.Where(d => d.Id == actor.HomeDepartmentId);
        Departments = await departments.OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToListAsync();
        if (SupportDepartmentId == 0 && Departments.Count == 1
            && int.TryParse(Departments[0].Value, out var defaultDepartmentId))
        {
            SupportDepartmentId = defaultDepartmentId;
        }
        Items = await (await intakes.PendingAsync()).AsNoTracking().Include(x => x.SupportDepartment)
            .OrderBy(x => x.ReceivedAt).Take(200).ToListAsync();
        var emails = Items.Select(x => x.SenderEmail.ToUpperInvariant()).Distinct().ToArray();
        var users = await db.Users.AsNoTracking().Where(x => x.IsActive && x.NormalizedEmail != null
            && emails.Contains(x.NormalizedEmail)).ToListAsync();
        MatchingUsers = Items.ToDictionary(x => x.Id, x => users
            .Where(u => string.Equals(u.Email, x.SenderEmail, StringComparison.OrdinalIgnoreCase))
            .Select(u => new SelectListItem(u.FullName + " (" + u.Email + ")", u.Id)).ToList());
        return true;
    }
}
