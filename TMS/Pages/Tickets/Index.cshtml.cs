using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Tickets;

[Authorize]
public class IndexModel(ApplicationDbContext db, TicketService tickets, AccessService access,
    IConfiguration configuration) : PageModel
{
    public List<Ticket> Items { get; private set; } = [];
    public string Filter { get; private set; } = "mine";
    public bool CanWorkTickets { get; private set; }
    public bool MailIntakeEnabled { get; } = configuration.GetValue<bool>($"{TicketMailOptions.SectionName}:Enabled");
    public string? Search { get; private set; }
    public int PageNumber { get; private set; } = 1;
    public int PageCount { get; private set; }
    public int TotalCount { get; private set; }
    public int OpenCount { get; private set; }
    public int AssignedCount { get; private set; }
    public int ReviewCount { get; private set; }
    public int PoolCount { get; private set; }
    public int SlaBreachCount { get; private set; }
    private const int PageSize = 25;

    public async Task<IActionResult> OnGetAsync(string? filter, string? search, int pageNumber = 1)
    {
        var actor = await access.ActorAsync();
        var departmentId = actor.HomeDepartmentId;
        CanWorkTickets = actor.IsAdmin || departmentId.HasValue
            && await db.Departments.AnyAsync(d => d.Id == departmentId && d.IsTicketSupport && !d.IsDeleted);
        Filter = filter == "viewing" ? "viewing"
            : CanWorkTickets && (filter is "all" or "pool" or "assigned" or "following" or "review" or "sla" or "resolved" or "closed")
                ? filter! : "mine";
        Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (Search?.Length > 200) Search = Search[..200];
        var visible = await tickets.VisibleAsync();
        var workspace = CanWorkTickets
            ? visible.Where(t => actor.IsAdmin || t.SupportDepartmentId == departmentId)
            : visible.Where(t => t.RequesterUserId == actor.UserId || t.Viewers.Any(v => v.UserId == actor.UserId));
        OpenCount = await workspace.CountAsync(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        AssignedCount = await workspace.CountAsync(t => t.AssignedToUserId == actor.UserId && t.Status != TicketStatus.Closed);
        ReviewCount = await workspace.CountAsync(t => t.HasNewReply);
        PoolCount = CanWorkTickets
            ? await workspace.CountAsync(t => t.AssignedToUserId == null && t.Status != TicketStatus.Closed)
            : 0;
        SlaBreachCount = CanWorkTickets ? await workspace.CountAsync(t => t.SlaDueAt < DateTime.UtcNow
            && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed) : 0;
        var query = visible;
        query = Filter switch
        {
            "all" => query.Where(t => actor.IsAdmin || t.SupportDepartmentId == departmentId),
            "pool" => query.Where(t => t.AssignedToUserId == null && t.Status != TicketStatus.Closed
                && (actor.IsAdmin || t.SupportDepartmentId == departmentId)),
            "assigned" => query.Where(t => t.AssignedToUserId == actor.UserId),
            "following" => query.Where(t => t.Viewers.Any(v => v.UserId == actor.UserId && v.IsFollowing)),
            "viewing" => query.Where(t => t.Viewers.Any(v => v.UserId == actor.UserId)),
            "review" => query.Where(t => t.HasNewReply && (actor.IsAdmin || t.SupportDepartmentId == departmentId)),
            "sla" => query.Where(t => t.SlaDueAt < DateTime.UtcNow && t.Status != TicketStatus.Resolved
                && t.Status != TicketStatus.Closed && (actor.IsAdmin || t.SupportDepartmentId == departmentId)),
            "resolved" => query.Where(t => t.Status == TicketStatus.Resolved && (actor.IsAdmin || t.SupportDepartmentId == departmentId)),
            "closed" => query.Where(t => t.Status == TicketStatus.Closed && (actor.IsAdmin || t.SupportDepartmentId == departmentId)),
            _ => query.Where(t => t.RequesterUserId == actor.UserId || t.Viewers.Any(v => v.UserId == actor.UserId && v.IsFollowing))
        };
        if (Search is not null)
            query = query.Where(t => t.Subject.Contains(Search) || t.Requester.FullName.Contains(Search)
                || t.Requester.Email != null && t.Requester.Email.Contains(Search));
        TotalCount = await query.CountAsync();
        PageCount = (int)Math.Ceiling(TotalCount / (double)PageSize);
        PageNumber = Math.Clamp(pageNumber, 1, Math.Max(1, PageCount));
        var listQuery = query.AsNoTracking().Include(t => t.Requester).Include(t => t.AssignedToUser);
        Items = Filter == "pool"
            ? await listQuery.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
                .Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync()
            : await listQuery.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id)
                .Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostClaimAsync(int id)
    {
        var actor = await access.ActorAsync();
        await tickets.AssignAsync(id, actor.UserId);
        return RedirectToPage(new { filter = "assigned" });
    }
}
