using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AuditLogsModel : PageModel
{
    private readonly AuditLogService _auditLogService;

    public AuditLogsModel(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public List<AuditLog> Logs { get; set; } = new();

    public async Task OnGetAsync()
    {
        Logs = await _auditLogService.GetAllLogsAsync();
    }
}
