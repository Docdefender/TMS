using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Notifications;

[Authorize]
public class IndexModel(NotificationService notifications) : PageModel
{
    public List<Notification> Items { get; private set; } = [];
    public async Task OnGetAsync() => Items = await notifications.LatestAsync(100);
    public async Task<IActionResult> OnPostReadAsync(int id) { await notifications.MarkReadAsync(id); return RedirectToPage(); }
    public async Task<IActionResult> OnPostReadAllAsync() { await notifications.MarkAllReadAsync(); return RedirectToPage(); }
}
