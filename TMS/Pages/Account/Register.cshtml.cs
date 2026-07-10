using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TMS.Pages.Account;

// Registration is admin-only via Admin/Users page.
// This page is disabled — redirect to login.
public class RegisterModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Account/Login");
    public IActionResult OnPost() => RedirectToPage("/Account/Login");
}
