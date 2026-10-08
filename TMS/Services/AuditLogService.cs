using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class AuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, string entityType, int? entityId, string? userId,
        string? details = null, int? projectId = null)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ProjectId = projectId,
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = details
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetAllLogsAsync()
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<AuditLogPageResult> GetLogsPageAsync(
        string? search,
        string? action,
        string? entityType,
        int pageNumber,
        int pageSize)
    {
        var source = _context.AuditLogs.AsNoTracking();
        var actionOptions = await source.Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync();
        var entityOptions = await source.Select(x => x.EntityType).Distinct().OrderBy(x => x).ToListAsync();

        var query = source.AsQueryable();
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(x => x.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Details ?? string.Empty, pattern) ||
                EF.Functions.Like(x.Action, pattern) ||
                EF.Functions.Like(x.EntityType, pattern) ||
                (x.User != null && (EF.Functions.Like(x.User.FullName, pattern) ||
                                    EF.Functions.Like(x.User.Email ?? string.Empty, pattern))));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Include(x => x.User)
            .OrderByDescending(x => x.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AuditLogPageResult(items, totalCount, actionOptions, entityOptions);
    }

    public async Task<List<AuditLog>> GetLogsByEntityAsync(string entityType, int entityId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }
}

public sealed record AuditLogPageResult(
    List<AuditLog> Items,
    int TotalCount,
    List<string> Actions,
    List<string> EntityTypes);
