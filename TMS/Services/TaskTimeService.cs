using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public class TaskTimeService(ApplicationDbContext db, AccessService access)
{
    public async Task<List<TaskTimeEntry>> GetAsync(int taskId)
    {
        AccessService.Require((await access.TaskAsync(taskId)).View);
        return await db.TaskTimeEntries.AsNoTracking().Include(x => x.User)
            .Where(x => x.TaskItemId == taskId).OrderByDescending(x => x.StartedAt).ToListAsync();
    }

    public async Task StartAsync(int taskId, string? note)
    {
        AccessService.Require((await access.TaskAsync(taskId)).Contribute);
        var actor = await access.ActorAsync();
        if (await db.TaskTimeEntries.AnyAsync(x => x.UserId == actor.UserId && x.EndedAt == null))
            throw new InvalidOperationException("Önce devam eden zaman kaydınızı durdurun.");
        db.TaskTimeEntries.Add(new TaskTimeEntry { TaskItemId = taskId, UserId = actor.UserId,
            Note = NormalizeNote(note) });
        await db.SaveChangesAsync();
    }

    public async Task StopAsync(int taskId)
    {
        AccessService.Require((await access.TaskAsync(taskId)).Contribute);
        var actor = await access.ActorAsync();
        var entry = await db.TaskTimeEntries.FirstOrDefaultAsync(x => x.TaskItemId == taskId
            && x.UserId == actor.UserId && x.EndedAt == null)
            ?? throw new InvalidOperationException("Bu görevde çalışan bir sayaç bulunamadı.");
        entry.EndedAt = DateTime.UtcNow;
        entry.DurationMinutes = Math.Max(1, (int)Math.Ceiling((entry.EndedAt.Value - entry.StartedAt).TotalMinutes));
        await db.SaveChangesAsync();
    }

    public async Task AddManualAsync(int taskId, int minutes, DateTime? workedOn, string? note)
    {
        AccessService.Require((await access.TaskAsync(taskId)).Contribute);
        if (minutes is < 1 or > 1440) throw new InvalidOperationException("Süre 1 ile 1440 dakika arasında olmalıdır.");
        var actor = await access.ActorAsync();
        var localStart = (workedOn ?? DateTime.Today).Date.AddHours(12);
        var start = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();
        db.TaskTimeEntries.Add(new TaskTimeEntry { TaskItemId = taskId, UserId = actor.UserId,
            StartedAt = start, EndedAt = start.AddMinutes(minutes), DurationMinutes = minutes,
            Note = NormalizeNote(note), IsManual = true });
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int taskId, int entryId)
    {
        AccessService.Require((await access.TaskAsync(taskId)).View);
        var actor = await access.ActorAsync();
        var entry = await db.TaskTimeEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.TaskItemId == taskId)
            ?? throw new InvalidOperationException("Zaman kaydı bulunamadı.");
        AccessService.Require(actor.IsAdmin || entry.UserId == actor.UserId);
        db.TaskTimeEntries.Remove(entry);
        await db.SaveChangesAsync();
    }

    private static string? NormalizeNote(string? note)
    {
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note?.Length > 300) throw new InvalidOperationException("Zaman kaydı notu en fazla 300 karakter olabilir.");
        return note;
    }
}
