using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly DashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(DashboardService dashboardService, UserManager<ApplicationUser> userManager)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        public DashboardViewModel Dashboard { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return RedirectToPage("/Account/Login");
            }

            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");
            Dashboard = await _dashboardService.GetDashboardAsync(userId, isAdminOrManager);
            return Page();

       }

   }
}

