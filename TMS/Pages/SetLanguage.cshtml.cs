using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TMS.Pages;

public class SetLanguageModel : PageModel
{
    public IActionResult OnGet(string culture, string returnUrl = "/")
    {
        if (string.IsNullOrEmpty(culture))
        {
            return LocalRedirect(returnUrl);
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
        );

        return LocalRedirect(returnUrl);
    }
}
