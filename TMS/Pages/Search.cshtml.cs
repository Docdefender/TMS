using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages;

[Authorize]
public class SearchModel(AccessService access, TicketService tickets) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Tasks/Index");

    public async Task<IActionResult> OnGetSuggestionsAsync(string? q, string? scope)
    {
        var term = q?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return new JsonResult(Array.Empty<SearchSuggestion>());

        if (term.Length > 100) term = term[..100];
        scope = scope?.ToLowerInvariant();
        var results = new List<SearchSuggestion>();
        var take = scope == "all" ? 3 : 7;

        if (scope is "projects" or "all")
            results.AddRange(await ProjectSuggestionsAsync(term, take));
        if (scope is "tasks" or "all")
            results.AddRange(await TaskSuggestionsAsync(term, take));
        if (scope is "tickets" or "all")
            results.AddRange(await TicketSuggestionsAsync(term, take));

        return new JsonResult(results.Take(scope == "all" ? 8 : 7));
    }

    private async Task<List<SearchSuggestion>> ProjectSuggestionsAsync(string term, int take)
    {
        var query = await access.ProjectsAsync();
        var items = await query.AsNoTracking()
            .Where(x => x.Name.Contains(term) || x.Description != null && x.Description.Contains(term))
            .Include(x => x.Department).Include(x => x.Category).Include(x => x.Manager)
            .OrderBy(x => x.Name).Take(take)
            .Select(x => new { x.Id, x.Name, x.Description, x.Status, x.StartDate, x.EndDate,
                Department = x.Department != null ? x.Department.Name : null,
                Category = x.Category != null ? x.Category.Name : null,
                Manager = x.Manager != null ? x.Manager.FullName : null,
                MemberCount = x.Members.Count })
            .ToListAsync();

        return items.Select(x => new SearchSuggestion(
            "Proje", $"PRJ-{x.Id:D4}", x.Name, ProjectStatusText(x.Status),
            $"{x.Department ?? "Departman yok"} · {x.Category ?? "Kategori yok"}",
            x.Description ?? "Açıklama eklenmemiş.",
            Url.Page("/Projects/Details", new { id = x.Id })!,
            [new("Proje sorumlusu", x.Manager ?? "Atanmamış"),
             new("Başlangıç", x.StartDate.ToString("dd MMM yyyy")),
             new("Bitiş", x.EndDate?.ToString("dd MMM yyyy") ?? "Belirtilmedi"),
             new("Ekip", $"{x.MemberCount} kişi")],
            Url.Page("/Pipeline/Details", new { id = x.Id }), "Pipeline", null)).ToList();
    }

    private async Task<List<SearchSuggestion>> TaskSuggestionsAsync(string term, int take)
    {
        var query = await access.TasksAsync();
        var items = await query.AsNoTracking()
            .Where(x => x.Title.Contains(term) || x.Description != null && x.Description.Contains(term))
            .Include(x => x.Project).Include(x => x.AssignedToUser).Include(x => x.Category)
            .OrderByDescending(x => x.CreatedDate).Take(take)
            .Select(x => new { x.Id, x.Title, x.Description, x.Status, x.Priority, x.DueDate, x.ProjectId,
                Project = x.Project.Name,
                Assignee = x.AssignedToUser != null ? x.AssignedToUser.FullName : null,
                Category = x.Category != null ? x.Category.Name : null })
            .ToListAsync();

        return items.Select(x => new SearchSuggestion(
            "Görev", $"TSK-{x.Id:D5}", x.Title, TaskStatusText(x.Status), x.Project,
            x.Description ?? "Açıklama eklenmemiş.",
            Url.Page("/Tasks/Details", new { id = x.Id })!,
            [new("Sorumlu", x.Assignee ?? "Atanmamış"),
             new("Bitiş tarihi", x.DueDate?.ToString("dd MMM yyyy") ?? "Belirtilmedi"),
             new("Öncelik", PriorityText(x.Priority)),
             new("Kategori", x.Category ?? "Kategori yok")],
            null, null,
            new RelatedSuggestion(Url.Page("/Projects/Details", new { id = x.ProjectId })!,
                $"PRJ-{x.ProjectId:D4}", x.Project, "Bu görevin bağlı olduğu proje"))).ToList();
    }

    private async Task<List<SearchSuggestion>> TicketSuggestionsAsync(string term, int take)
    {
        var query = await tickets.VisibleAsync();
        var items = await query.AsNoTracking()
            .Where(x => x.Subject.Contains(term) || x.Requester.FullName.Contains(term)
                || x.Requester.Email != null && x.Requester.Email.Contains(term))
            .Include(x => x.Requester).Include(x => x.AssignedToUser)
            .OrderByDescending(x => x.UpdatedAt).Take(take)
            .Select(x => new { x.Id, x.Subject, x.Status, x.CreatedAt, x.UpdatedAt, x.HasNewReply,
                Requester = x.Requester.FullName,
                Assignee = x.AssignedToUser != null ? x.AssignedToUser.FullName : null })
            .ToListAsync();

        return items.Select(x => new SearchSuggestion(
            "Ticket", $"TCK-{x.CreatedAt.Year}-{x.Id:D6}", x.Subject, TicketStatusText(x.Status),
            $"Talep sahibi · {x.Requester}",
            x.HasNewReply ? "Yeni yanıt içeren Ticket. Konuşmayı ayrıntı sayfasından inceleyebilirsiniz."
                : "Ticket konuşmasını ve işlem geçmişini ayrıntı sayfasından inceleyebilirsiniz.",
            Url.Page("/Tickets/Details", new { id = x.Id })!,
            [new("Talep sahibi", x.Requester), new("Sorumlu", x.Assignee ?? "Havuzda"),
             new("Oluşturma", x.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm")),
             new("Son işlem", x.UpdatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"))],
            null, null, null)).ToList();
    }

    private static string ProjectStatusText(ProjectStatus status) => status switch
    {
        ProjectStatus.NotStarted => "Başlamadı", ProjectStatus.InProgress => "Devam ediyor",
        ProjectStatus.Completed => "Tamamlandı", ProjectStatus.OnHold => "Beklemede",
        ProjectStatus.Cancelled => "İptal", _ => status.ToString()
    };

    private static string TaskStatusText(Models.TaskStatus status) => status switch
    {
        Models.TaskStatus.ToDo => "Yapılacak", Models.TaskStatus.InProgress => "Devam ediyor",
        Models.TaskStatus.InReview => "Gözden geçiriliyor", Models.TaskStatus.Done => "Tamamlandı",
        _ => status.ToString()
    };

    private static string PriorityText(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "Düşük", TaskPriority.Normal => "Normal", TaskPriority.High => "Yüksek",
        TaskPriority.Critical => "Kritik", _ => priority.ToString()
    };

    private static string TicketStatusText(TicketStatus status) => status switch
    {
        TicketStatus.Open => "Açık", TicketStatus.InProgress => "İşlemde",
        TicketStatus.WaitingForInformation => "Bilgi bekleniyor", TicketStatus.Resolved => "Çözüldü",
        TicketStatus.Closed => "Kapatıldı", _ => status.ToString()
    };
}

public record SearchMeta(string Label, string Value);
public record RelatedSuggestion(string Url, string Key, string Title, string Subtitle);
public record SearchSuggestion(string Type, string Key, string Title, string Status, string Subtitle,
    string Description, string DetailUrl, IReadOnlyList<SearchMeta> Meta, string? SecondaryUrl,
    string? SecondaryLabel, RelatedSuggestion? Related);
