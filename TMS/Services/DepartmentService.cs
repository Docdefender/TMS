using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class DepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Department>> GetAllAsync()
    {
        return await _context.Departments.OrderBy(d => d.Name).ToListAsync();
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _context.Departments.FindAsync(id);
    }

    public async Task CreateAsync(Department department)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Department department)
    {
        _context.Departments.Update(department);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department is not null)
        {
            department.IsDeleted = true;
            department.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Department>> GetDeletedAsync()
    {
        return await _context.Departments
            .IgnoreQueryFilters()
            .Where(d => d.IsDeleted)
            .OrderByDescending(d => d.DeletedAt)
            .ToListAsync();
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var department = await _context.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted);
        if (department is null) return false;

        department.IsDeleted = false;
        department.DeletedAt = null;
        department.DeletedByUserId = null;
        await _context.SaveChangesAsync();
        return true;
    }
}
