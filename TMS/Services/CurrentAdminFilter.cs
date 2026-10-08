using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TMS.Services;

// Cookie roles can be stale after a role change. Admin operations always check the current role.
public class CurrentAdminFilter(AccessService access) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.ActionDescriptor.ViewEnginePath.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase)
            && !(await access.ActorAsync()).IsAdmin)
        { context.Result = new ForbidResult(); return; }
        await next();
    }
}
