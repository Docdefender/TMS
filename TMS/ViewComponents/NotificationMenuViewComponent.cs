using Microsoft.AspNetCore.Mvc;
using TMS.Services;

namespace TMS.ViewComponents;

public class NotificationMenuViewComponent(NotificationService notifications) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View(new NotificationMenuModel(await notifications.LatestAsync(), await notifications.UnreadCountAsync()));
}

public record NotificationMenuModel(IReadOnlyList<TMS.Models.Notification> Items, int UnreadCount);
