using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class NotificationService(ApplicationDbContext db, AccessService access)
{
    public async Task CreateAsync(string? userId, string type, string title, string message,
        string? url = null, string? actorUserId = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId == actorUserId) return;
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.IsActive)) return;
        db.Notifications.Add(new Notification { UserId = userId, ActorUserId = actorUserId,
            Type = type, Title = title, Message = message, Url = url });
        await db.SaveChangesAsync();
    }

    public async Task<List<Notification>> LatestAsync(int take = 8)
    {
        var actor = await access.ActorAsync();
        return await db.Notifications.AsNoTracking().Include(x => x.ActorUser)
            .Where(x => x.UserId == actor.UserId).OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id).Take(Math.Clamp(take, 1, 100)).ToListAsync();
    }

    public async Task<int> UnreadCountAsync()
    {
        var actor = await access.ActorAsync();
        return await db.Notifications.CountAsync(x => x.UserId == actor.UserId && x.ReadAt == null);
    }

    public async Task MarkReadAsync(int id)
    {
        var actor = await access.ActorAsync();
        var item = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == actor.UserId);
        if (item is null || item.ReadAt.HasValue) return;
        item.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync()
    {
        var actor = await access.ActorAsync();
        await db.Notifications.Where(x => x.UserId == actor.UserId && x.ReadAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(n => n.ReadAt, DateTime.UtcNow));
    }
}
