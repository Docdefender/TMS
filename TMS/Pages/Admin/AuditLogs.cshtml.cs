using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public List<string> ActionOptions { get; set; } = new();
    public List<string> EntityOptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public int PageSize => 25;
    public int StartItem => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int EndItem => Math.Min(PageNumber * PageSize, TotalCount);
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Action) || !string.IsNullOrWhiteSpace(EntityType);

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        var result = await _auditLogService.GetLogsPageAsync(Search, Action, EntityType, PageNumber, PageSize);
        TotalCount = result.TotalCount;
        ActionOptions = result.Actions;
        EntityOptions = result.EntityTypes;

        if (PageNumber > PageCount)
        {
            PageNumber = PageCount;
            result = await _auditLogService.GetLogsPageAsync(Search, Action, EntityType, PageNumber, PageSize);
        }

        Logs = result.Items;
    }
}
