using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Pipeline;

[Authorize]
public class DetailsModel(PipelineService pipeline) : PageModel
{
    public ProjectPipelineView Pipeline { get; private set; } = null!;
    public List<PipelineHistoryEntry> History { get; private set; } = [];

    [BindProperty, Required, StringLength(120)] public string StageName { get; set; } = string.Empty;
    [BindProperty, StringLength(500)] public string? StageDescription { get; set; }
    [BindProperty, Required, StringLength(120)] public string CheckpointName { get; set; } = string.Empty;
    [BindProperty, StringLength(500)] public string? CheckpointDescription { get; set; }
    [BindProperty] public bool RequiresApproval { get; set; }
    [BindProperty] public int RelationTargetProjectId { get; set; }
    [BindProperty] public ProjectRelationType RelationType { get; set; }
    [BindProperty] public int? RelationCheckpointId { get; set; }
    [BindProperty(SupportsGet = true)] public string? ViewMode { get; set; } = "timeline";
    [BindProperty(SupportsGet = true)] public string? Scale { get; set; } = "week";
    [BindProperty(SupportsGet = true)] public string? HistoryFilter { get; set; } = "all";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ViewMode = ViewMode is "flow" or "history" ? ViewMode : "timeline";
        Scale = Scale is "day" or "month" ? Scale : "week";
        HistoryFilter = HistoryFilter is "structure" or "tasks" or "relations" ? HistoryFilter : "all";
        return await LoadAsync(id) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAddStageAsync(int id)
    {
        ModelState.Remove(nameof(CheckpointName));
        if (!ModelState.IsValid) return await LoadAsync(id) ? Page() : NotFound();
        await pipeline.AddStageAsync(id, StageName, StageDescription);
        TempData["PipelineMessage"] = "Aşama eklendi.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostAddCheckpointAsync(int id, int stageId)
    {
        ModelState.Remove(nameof(StageName));
        if (!ModelState.IsValid) return await LoadAsync(id) ? Page() : NotFound();
        await pipeline.AddCheckpointAsync(id, stageId, CheckpointName, RequiresApproval, CheckpointDescription);
        TempData["PipelineMessage"] = "Checkpoint eklendi.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostUpdateStageAsync(int id, int stageId, string stageName, string? stageDescription)
    {
        try { await pipeline.UpdateStageAsync(id, stageId, stageName, stageDescription); TempData["PipelineMessage"] = "Aşama güncellendi."; }
        catch (InvalidOperationException ex) { TempData["PipelineMessage"] = ex.Message; }
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostMoveStageAsync(int id, int stageId, int direction)
    {
        await pipeline.MoveStageAsync(id, stageId, direction);
        TempData["PipelineMessage"] = "Aşama sırası güncellendi.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostDeleteStageAsync(int id, int stageId)
    {
        await pipeline.DeleteStageAsync(id, stageId);
        TempData["PipelineMessage"] = "Aşama silindi; görevleri bekleyen alana taşındı.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostUpdateCheckpointAsync(int id, int checkpointId, string checkpointName,
        string? checkpointDescription, bool requiresApproval)
    {
        try
        {
            await pipeline.UpdateCheckpointAsync(id, checkpointId, checkpointName, checkpointDescription, requiresApproval);
            TempData["PipelineMessage"] = "Checkpoint güncellendi.";
        }
        catch (InvalidOperationException ex) { TempData["PipelineMessage"] = ex.Message; }
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostMoveCheckpointAsync(int id, int checkpointId, int direction)
    {
        await pipeline.MoveCheckpointAsync(id, checkpointId, direction);
        TempData["PipelineMessage"] = "Checkpoint sırası güncellendi.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostDeleteCheckpointAsync(int id, int checkpointId)
    {
        await pipeline.DeleteCheckpointAsync(id, checkpointId);
        TempData["PipelineMessage"] = "Checkpoint silindi; görevleri aşamada tutuldu.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostAssignTaskAsync(int id, int taskId, int? stageId, int? checkpointId)
    {
        await pipeline.AssignTaskAsync(id, taskId, stageId, checkpointId);
        TempData["PipelineMessage"] = "Görevin pipeline konumu güncellendi.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostAddTaskDependencyAsync(int id, int taskId, int prerequisiteTaskId)
    {
        try
        {
            await pipeline.AddTaskDependencyAsync(id, taskId, prerequisiteTaskId);
            TempData["PipelineMessage"] = "Görev ön koşulu eklendi.";
        }
        catch (InvalidOperationException ex) { TempData["PipelineMessage"] = ex.Message; }
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostRemoveTaskDependencyAsync(int id, int dependencyId)
    {
        await pipeline.RemoveTaskDependencyAsync(id, dependencyId);
        TempData["PipelineMessage"] = "Görev ön koşulu kaldırıldı.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostApproveCheckpointAsync(int id, int checkpointId)
    {
        await pipeline.ApproveCheckpointAsync(id, checkpointId);
        TempData["PipelineMessage"] = "Checkpoint onaylandı.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostUpdateScheduleAsync(int id, int taskId, DateTime? plannedStartDate, DateTime? plannedEndDate, string scale = "week")
    {
        try
        {
            await pipeline.UpdateTaskScheduleAsync(id, taskId, plannedStartDate, plannedEndDate);
            TempData["PipelineMessage"] = "Görev takvimi güncellendi.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["PipelineMessage"] = ex.Message;
        }
        return RedirectToPage(new { id, viewMode = "timeline", scale });
    }

    public async Task<IActionResult> OnPostAddRelationAsync(int id)
    {
        try
        {
            await pipeline.AddProjectRelationAsync(id, RelationTargetProjectId, RelationType, RelationCheckpointId);
            TempData["PipelineMessage"] = "Proje bağlantısı eklendi.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["PipelineMessage"] = ex.Message;
        }
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostRemoveRelationAsync(int id, int relationId)
    {
        await pipeline.RemoveProjectRelationAsync(id, relationId);
        TempData["PipelineMessage"] = "Proje bağlantısı kaldırıldı.";
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    public async Task<IActionResult> OnPostUpdateRelationCheckpointAsync(int id, int relationId, int? relationCheckpointId)
    {
        try
        {
            await pipeline.UpdateProjectRelationCheckpointAsync(id, relationId, relationCheckpointId);
            TempData["PipelineMessage"] = "Bağımlılığın checkpoint bağlantısı güncellendi.";
        }
        catch (InvalidOperationException ex) { TempData["PipelineMessage"] = ex.Message; }
        return RedirectToPage(new { id, viewMode = "flow" });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var result = await pipeline.GetProjectAsync(id);
        if (result is null) return false;
        Pipeline = result;
        if (ViewMode == "history")
            History = await pipeline.GetHistoryAsync(id, HistoryFilter) ?? [];
        return true;
    }
}
