using TMS.Models;

namespace TMS.Services;

public static class TicketSla
{
    public static DateTime DefaultDueAt(DateTime createdAt, TicketPriority priority) =>
        createdAt.AddHours(priority switch
        {
            TicketPriority.Critical => 8,
            TicketPriority.High => 24,
            TicketPriority.Normal => 72,
            TicketPriority.Low => 120,
            _ => 72
        });

    public static bool IsFinished(TicketStatus status) => status is TicketStatus.Resolved or TicketStatus.Closed;
    public static bool IsBreached(Ticket ticket, DateTime utcNow) =>
        !IsFinished(ticket.Status) && ticket.SlaDueAt.HasValue && ticket.SlaDueAt.Value < utcNow;

    public static string RemainingText(Ticket ticket, DateTime utcNow)
    {
        if (!ticket.SlaDueAt.HasValue) return "Hedef yok";
        if (IsFinished(ticket.Status)) return ticket.SlaBreachedAt.HasValue ? "SLA aşıldı" : "SLA içinde";
        var difference = ticket.SlaDueAt.Value - utcNow;
        var prefix = difference < TimeSpan.Zero ? "aşıldı" : "kaldı";
        difference = difference.Duration();
        if (difference.TotalDays >= 1) return $"{(int)difference.TotalDays} gün {difference.Hours} sa {prefix}";
        if (difference.TotalHours >= 1) return $"{(int)difference.TotalHours} sa {difference.Minutes} dk {prefix}";
        return $"{Math.Max(1, difference.Minutes)} dk {prefix}";
    }
}
