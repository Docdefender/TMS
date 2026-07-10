using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class CategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task CreateAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category is not null)
        {
            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Category>> GetDeletedAsync()
    {
        return await _context.Categories
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted)
            .OrderByDescending(c => c.DeletedAt)
            .ToListAsync();
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var category = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
        if (category is null) return false;

        category.IsDeleted = false;
        category.DeletedAt = null;
        category.DeletedByUserId = null;
        await _context.SaveChangesAsync();
        return true;
    }
}
