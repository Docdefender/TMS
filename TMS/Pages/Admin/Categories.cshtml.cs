using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CategoriesModel : PageModel
{
    private readonly CategoryService _categoryService;

    public CategoriesModel(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public List<Category> Categories { get; set; } = new();

    [BindProperty]
    public Category NewCategory { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _categoryService.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategory.Name))
        {
            ModelState.AddModelError("NewCategory.Name", "Name is required.");
            Categories = await _categoryService.GetAllAsync();
            return Page();
        }

        await _categoryService.CreateAsync(NewCategory);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _categoryService.DeleteAsync(id);
        return RedirectToPage();
    }
}
