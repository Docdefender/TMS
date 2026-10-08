using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public static class TicketAutoAssignment
{
    public static async Task<string?> SelectAssigneeAsync(ApplicationDbContext db, int supportDepartmentId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Departments.AnyAsync(x => x.Id == supportDepartmentId && x.IsTicketSupport
                && x.AutoAssignTickets && !x.IsDeleted, cancellationToken))
            return null;

        return await db.Users.Where(x => x.IsActive && x.DepartmentId == supportDepartmentId)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                Workload = db.Tickets.Count(t => t.AssignedToUserId == x.Id
                    && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
            })
            .OrderBy(x => x.Workload).ThenBy(x => x.FullName).ThenBy(x => x.Id)
            .Select(x => x.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
