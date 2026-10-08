using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

public record PipelineProjectSummary(Project Project, int TaskCount, int DoneTaskCount,
    int StageCount, int CheckpointCount, int CompletedCheckpointCount)
{
    public int Progress => TaskCount == 0 ? 0 : (int)Math.Round(DoneTaskCount * 100d / TaskCount);
}

public record ProjectPipelineView(Project Project, List<PipelineStage> Stages,
    List<TaskItem> UnassignedTasks, bool CanManage, List<ProjectRelation> Relations,
    List<Project> RelationCandidates)
{
    public int TaskCount => Stages.Sum(x => x.Tasks.Count) + UnassignedTasks.Count;
    public int DoneTaskCount => Stages.Sum(x => x.Tasks.Count(t => t.Status == Models.TaskStatus.Done))
        + UnassignedTasks.Count(t => t.Status == Models.TaskStatus.Done);
    public int Progress => TaskCount == 0 ? 0 : (int)Math.Round(DoneTaskCount * 100d / TaskCount);
    public List<TaskItem> AllTasks => Stages.SelectMany(x => x.Tasks).Concat(UnassignedTasks)
        .DistinctBy(x => x.Id).OrderBy(x => x.Title).ToList();
}

public record PipelineHistoryEntry(AuditLog Log, string Title, string Category, string Icon, string? Description);

public class PipelineService(ApplicationDbContext db, AccessService access, AuditLogService audit)
{
    private static readonly Dictionary<string, (string Title, string Category, string Icon)> HistoryActions = new()
    {
        ["PipelineStageCreated"] = ("Aşama oluşturuldu", "structure", "bi-plus-circle"),
        ["PipelineStageUpdated"] = ("Aşama güncellendi", "structure", "bi-pencil"),
        ["PipelineStageMoved"] = ("Aşama sırası değiştirildi", "structure", "bi-arrow-down-up"),
        ["PipelineStageDeleted"] = ("Aşama silindi", "structure", "bi-trash"),
        ["PipelineCheckpointCreated"] = ("Checkpoint oluşturuldu", "structure", "bi-flag"),
        ["PipelineCheckpointUpdated"] = ("Checkpoint güncellendi", "structure", "bi-pencil"),
        ["PipelineCheckpointMoved"] = ("Checkpoint sırası değiştirildi", "structure", "bi-arrow-down-up"),
        ["PipelineCheckpointDeleted"] = ("Checkpoint silindi", "structure", "bi-trash"),
        ["PipelineCheckpointApproved"] = ("Checkpoint onaylandı", "structure", "bi-check-circle"),
        ["PipelineTaskAssigned"] = ("Görev pipeline içinde taşındı", "tasks", "bi-kanban"),
        ["TaskScheduleChanged"] = ("Görev takvimi güncellendi", "tasks", "bi-calendar-event"),
        ["StatusChanged"] = ("Görev durumu değişti", "tasks", "bi-check2-square"),
        ["TaskDependencyCreated"] = ("Görev bağımlılığı eklendi", "tasks", "bi-link-45deg"),
        ["TaskDependencyRemoved"] = ("Görev bağımlılığı kaldırıldı", "tasks", "bi-link-45deg"),
        ["ProjectRelationCreated"] = ("Proje bağlantısı eklendi", "relations", "bi-link-45deg"),
        ["ProjectRelationRemoved"] = ("Proje bağlantısı kaldırıldı", "relations", "bi-link-45deg"),
        ["ProjectDependencyCheckpointChanged"] = ("Bağımlılık checkpoint'i değiştirildi", "relations", "bi-signpost-split")
    };

    public static int TaskProgress(Models.TaskStatus status) => status switch
    {
        Models.TaskStatus.ToDo => 0,
        Models.TaskStatus.InProgress => 50,
        Models.TaskStatus.InReview => 80,
        Models.TaskStatus.Done => 100,
        _ => 0
    };

    public static bool IsCheckpointComplete(PipelineCheckpoint checkpoint)
    {
        var tasks = checkpoint.Tasks.Where(x => !x.IsDeleted).ToList();
        return tasks.Count > 0 && tasks.All(x => x.Status == Models.TaskStatus.Done)
            && checkpoint.BlockingProjectRelations.All(x => x.TargetProject.Status == ProjectStatus.Completed)
            && (!checkpoint.RequiresApproval || checkpoint.ApprovedAt.HasValue);
    }

    public async Task<List<PipelineProjectSummary>> GetProjectsAsync()
    {
        var projects = await (await access.ProjectsAsync())
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .Include(x => x.Tasks)
            .Include(x => x.PipelineStages).ThenInclude(x => x.Checkpoints).ThenInclude(x => x.Tasks)
            .Include(x => x.PipelineStages).ThenInclude(x => x.Checkpoints)
                .ThenInclude(x => x.BlockingProjectRelations).ThenInclude(x => x.TargetProject)
            .AsSplitQuery()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var visibleTaskIds = (await (await access.TasksAsync()).Select(x => x.Id).ToListAsync()).ToHashSet();
        foreach (var project in projects)
        {
            project.Tasks = project.Tasks.Where(x => visibleTaskIds.Contains(x.Id)).ToList();
            foreach (var checkpoint in project.PipelineStages.SelectMany(x => x.Checkpoints))
                checkpoint.Tasks = checkpoint.Tasks.Where(x => visibleTaskIds.Contains(x.Id)).ToList();
        }

        return projects.Select(p =>
        {
            var tasks = p.Tasks.Where(x => !x.IsDeleted).ToList();
            var checkpoints = p.PipelineStages.SelectMany(x => x.Checkpoints).ToList();
            return new PipelineProjectSummary(p, tasks.Count,
                tasks.Count(x => x.Status == Models.TaskStatus.Done), p.PipelineStages.Count,
                checkpoints.Count, checkpoints.Count(IsCheckpointComplete));
        }).ToList();
    }

    public async Task<ProjectPipelineView?> GetProjectAsync(int projectId)
    {
        var rights = await access.ProjectAsync(projectId);
        if (!rights.View) return null;

        var project = await db.Projects.AsNoTracking()
            .Include(x => x.Department).Include(x => x.Manager).Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == projectId);
        if (project is null) return null;

        var stages = await db.PipelineStages.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .Include(x => x.Tasks.Where(t => !t.IsDeleted)).ThenInclude(x => x.AssignedToUser)
            .Include(x => x.Tasks.Where(t => !t.IsDeleted)).ThenInclude(x => x.PrerequisiteDependencies)
                .ThenInclude(x => x.PrerequisiteTask)
            .Include(x => x.Checkpoints.OrderBy(c => c.SortOrder)).ThenInclude(x => x.Tasks.Where(t => !t.IsDeleted))
            .Include(x => x.Checkpoints.OrderBy(c => c.SortOrder))
                .ThenInclude(x => x.BlockingProjectRelations).ThenInclude(x => x.TargetProject)
            .AsSplitQuery().OrderBy(x => x.SortOrder).ToListAsync();
        var visibleTaskIds = (await (await access.TasksAsync()).Where(x => x.ProjectId == projectId)
            .Select(x => x.Id).ToListAsync()).ToHashSet();
        foreach (var stage in stages)
        {
            stage.Tasks = stage.Tasks.Where(x => visibleTaskIds.Contains(x.Id)).ToList();
            foreach (var checkpoint in stage.Checkpoints)
                checkpoint.Tasks = checkpoint.Tasks.Where(x => visibleTaskIds.Contains(x.Id)).ToList();
        }
        var unassigned = await (await access.TasksAsync()).AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.PipelineStageId == null)
            .Include(x => x.AssignedToUser)
            .Include(x => x.PrerequisiteDependencies).ThenInclude(x => x.PrerequisiteTask)
            .OrderBy(x => x.DueDate).ToListAsync();

        var visibleProjectIds = await (await access.ProjectsAsync()).Select(x => x.Id).ToListAsync();
        var relations = await db.ProjectRelations.AsNoTracking()
            .Where(x => (x.SourceProjectId == projectId && visibleProjectIds.Contains(x.TargetProjectId))
                || (x.TargetProjectId == projectId && visibleProjectIds.Contains(x.SourceProjectId)))
            .Include(x => x.SourceProject).Include(x => x.TargetProject).Include(x => x.BlockingCheckpoint)
            .OrderBy(x => x.Type).ThenBy(x => x.SourceProject.Name).ThenBy(x => x.TargetProject.Name)
            .ToListAsync();
        var candidates = await (await access.ProjectsAsync()).AsNoTracking()
            .Where(x => x.Id != projectId).OrderBy(x => x.Name).ToListAsync();

        return new ProjectPipelineView(project, stages, unassigned,
            rights.ChangeStatus || (await access.ActorAsync()).IsAdmin, relations, candidates);
    }

    public async Task<List<PipelineHistoryEntry>?> GetHistoryAsync(int projectId, string? category = null, int take = 100)
    {
        if (!(await access.ProjectAsync(projectId)).View) return null;
        var actions = HistoryActions.Keys.ToArray();
        var query = db.AuditLogs.AsNoTracking().Include(x => x.User)
            .Where(x => x.ProjectId == projectId && actions.Contains(x.Action));
        var actor = await access.ActorAsync();
        var hasRestrictedTasks = !actor.IsAdmin && await db.Tickets.AnyAsync(x => x.LinkedTask != null
            && x.LinkedTask.ProjectId == projectId && x.SupportDepartmentId != actor.HomeDepartmentId);
        if (hasRestrictedTasks)
        {
            var taskActions = HistoryActions.Where(x => x.Value.Category == "tasks")
                .Select(x => x.Key).ToArray();
            query = query.Where(x => !taskActions.Contains(x.Action));
        }
        if (category is "structure" or "tasks" or "relations")
        {
            var categoryActions = HistoryActions.Where(x => x.Value.Category == category).Select(x => x.Key).ToArray();
            query = query.Where(x => categoryActions.Contains(x.Action));
        }
        var logs = await query.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id)
            .Take(Math.Clamp(take, 1, 250)).ToListAsync();
        return logs.Select(x =>
        {
            var display = HistoryActions[x.Action];
            return new PipelineHistoryEntry(x, display.Title, display.Category, display.Icon, HistoryDescription(x));
        }).ToList();
    }

    private static string? HistoryDescription(AuditLog log)
    {
        var name = QuotedName(log.Details);
        return log.Action switch
        {
            "PipelineStageCreated" => name is null ? null : $"“{name}” aşaması oluşturuldu.",
            "PipelineStageUpdated" => name is null ? null : $"“{name}” aşamasının bilgileri güncellendi.",
            "PipelineStageMoved" => "Aşamanın pipeline içindeki sırası değiştirildi.",
            "PipelineStageDeleted" => name is null ? "Aşama silindi; bağlı görevler bekleyen alana taşındı." : $"“{name}” aşaması silindi; bağlı görevler bekleyen alana taşındı.",
            "PipelineCheckpointCreated" => name is null ? null : $"“{name}” checkpoint'i oluşturuldu.",
            "PipelineCheckpointUpdated" => name is null ? null : $"“{name}” checkpoint'inin bilgileri güncellendi.",
            "PipelineCheckpointMoved" => "Checkpoint'in aşama içindeki sırası değiştirildi.",
            "PipelineCheckpointDeleted" => name is null ? "Checkpoint silindi; bağlı görevler aşamada tutuldu." : $"“{name}” checkpoint'i silindi; bağlı görevler aşamada tutuldu.",
            "PipelineCheckpointApproved" => name is null ? "Checkpoint yönetici tarafından onaylandı." : $"“{name}” checkpoint'i yönetici tarafından onaylandı.",
            "PipelineTaskAssigned" => name is null ? null : $"“{name}” görevinin aşama veya checkpoint konumu değiştirildi.",
            "TaskScheduleChanged" => name is null ? "Görevin planlanan tarihleri değiştirildi." : $"“{name}” görevinin planlanan tarihleri değiştirildi.",
            "StatusChanged" => name is null ? "Görevin çalışma durumu değiştirildi." : $"“{name}” görevinin çalışma durumu değiştirildi.",
            "TaskDependencyCreated" => "Göreve tamamlanması gereken bir ön koşul eklendi.",
            "TaskDependencyRemoved" => "Görevin ön koşullarından biri kaldırıldı.",
            "ProjectRelationCreated" => "Projeye yeni bir ilişki veya bağımlılık eklendi.",
            "ProjectRelationRemoved" => "Proje ilişkisi veya bağımlılığı kaldırıldı.",
            "ProjectDependencyCheckpointChanged" => "Bağımlılığın etkilediği checkpoint değiştirildi.",
            _ => log.Details
        };
    }

    private static string? QuotedName(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return null;
        var start = details.IndexOf('\'');
        if (start < 0) return null;
        var end = details.IndexOf('\'', start + 1);
        return end > start ? details[(start + 1)..end] : null;
    }

    public async Task AddTaskDependencyAsync(int projectId, int dependentTaskId, int prerequisiteTaskId)
    {
        await RequireManageAsync(projectId);
        AccessService.Require((await access.TaskAsync(dependentTaskId)).Edit
            && (await access.TaskAsync(prerequisiteTaskId)).View);
        if (dependentTaskId == prerequisiteTaskId)
            throw new InvalidOperationException("Bir görev kendisine bağlı olamaz.");
        var tasks = await db.TaskItems.Where(x => x.ProjectId == projectId
            && (x.Id == dependentTaskId || x.Id == prerequisiteTaskId)).ToListAsync();
        if (tasks.Count != 2) throw new InvalidOperationException("Her iki görev de bu projeye ait olmalıdır.");
        if (await db.TaskDependencies.AnyAsync(x => x.DependentTaskId == dependentTaskId
            && x.PrerequisiteTaskId == prerequisiteTaskId))
            throw new InvalidOperationException("Bu görev bağımlılığı zaten mevcut.");
        if (await CreatesTaskDependencyCycleAsync(projectId, dependentTaskId, prerequisiteTaskId))
            throw new InvalidOperationException("Bu bağımlılık görevler arasında döngü oluşturacağı için eklenemez.");
        var dependent = tasks.Single(x => x.Id == dependentTaskId);
        var prerequisite = tasks.Single(x => x.Id == prerequisiteTaskId);
        if (dependent.Status == Models.TaskStatus.Done && prerequisite.Status != Models.TaskStatus.Done)
            throw new InvalidOperationException("Tamamlanmış göreve, henüz tamamlanmamış bir ön koşul eklenemez.");
        var dependency = new TaskDependency
        {
            DependentTaskId = dependentTaskId,
            PrerequisiteTaskId = prerequisiteTaskId,
            CreatedByUserId = (await access.ActorAsync()).UserId
        };
        db.TaskDependencies.Add(dependency);
        await db.SaveChangesAsync();
        await audit.LogAsync("TaskDependencyCreated", "TaskDependency", dependency.Id, dependency.CreatedByUserId,
            $"Task '{dependent.Title}' now requires '{prerequisite.Title}'.", projectId);
    }

    public async Task RemoveTaskDependencyAsync(int projectId, int dependencyId)
    {
        await RequireManageAsync(projectId);
        var dependency = await db.TaskDependencies.Include(x => x.DependentTask).Include(x => x.PrerequisiteTask)
            .FirstOrDefaultAsync(x => x.Id == dependencyId && x.DependentTask.ProjectId == projectId)
            ?? throw new InvalidOperationException("Görev bağımlılığı bulunamadı.");
        AccessService.Require((await access.TaskAsync(dependency.DependentTaskId)).Edit
            && (await access.TaskAsync(dependency.PrerequisiteTaskId)).View);
        var details = $"Task '{dependency.DependentTask.Title}' no longer requires '{dependency.PrerequisiteTask.Title}'.";
        db.TaskDependencies.Remove(dependency);
        await db.SaveChangesAsync();
        await audit.LogAsync("TaskDependencyRemoved", "TaskDependency", dependencyId, (await access.ActorAsync()).UserId,
            details, projectId);
    }

    private async Task<bool> CreatesTaskDependencyCycleAsync(int projectId, int dependentTaskId, int prerequisiteTaskId)
    {
        var edges = await db.TaskDependencies.AsNoTracking()
            .Where(x => x.DependentTask.ProjectId == projectId)
            .Select(x => new { x.DependentTaskId, x.PrerequisiteTaskId }).ToListAsync();
        var prerequisites = edges.GroupBy(x => x.DependentTaskId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.PrerequisiteTaskId).ToArray());
        var pending = new Stack<int>();
        var visited = new HashSet<int>();
        pending.Push(prerequisiteTaskId);
        while (pending.TryPop(out var current))
        {
            if (current == dependentTaskId) return true;
            if (!visited.Add(current) || !prerequisites.TryGetValue(current, out var next)) continue;
            foreach (var taskId in next) pending.Push(taskId);
        }
        return false;
    }

    public async Task AddProjectRelationAsync(int projectId, int targetProjectId, ProjectRelationType type, int? blockingCheckpointId = null)
    {
        await RequireManageAsync(projectId);
        if (projectId == targetProjectId) throw new InvalidOperationException("Bir proje kendisiyle ilişkilendirilemez.");
        if (!Enum.IsDefined(type)) throw new InvalidOperationException("Geçersiz ilişki türü.");
        if (!await (await access.ProjectsAsync()).AnyAsync(x => x.Id == targetProjectId))
            throw new UnauthorizedAccessException("Seçilen projeye erişiminiz bulunmuyor.");
        if (type == ProjectRelationType.Related) blockingCheckpointId = null;
        if (blockingCheckpointId.HasValue && !await db.PipelineCheckpoints.AnyAsync(x => x.Id == blockingCheckpointId
            && x.PipelineStage.ProjectId == projectId))
            throw new InvalidOperationException("Bağımlılığın bağlanacağı checkpoint bu projeye ait değil.");

        var sourceId = projectId;
        var targetId = targetProjectId;
        if (type == ProjectRelationType.Related && sourceId > targetId)
            (sourceId, targetId) = (targetId, sourceId);

        if (await db.ProjectRelations.AnyAsync(x => x.SourceProjectId == sourceId
            && x.TargetProjectId == targetId && x.Type == type))
            throw new InvalidOperationException("Bu proje bağlantısı zaten mevcut.");

        if (type == ProjectRelationType.Dependency && await CreatesDependencyCycleAsync(sourceId, targetId))
            throw new InvalidOperationException("Bu bağımlılık döngü oluşturacağı için eklenemez.");

        var relation = new ProjectRelation
        {
            SourceProjectId = sourceId,
            TargetProjectId = targetId,
            Type = type,
            BlockingCheckpointId = blockingCheckpointId,
            CreatedByUserId = (await access.ActorAsync()).UserId
        };
        db.ProjectRelations.Add(relation);
        await db.SaveChangesAsync();
        await audit.LogAsync("ProjectRelationCreated", "ProjectRelation", relation.Id, relation.CreatedByUserId,
            $"Project relation {type} created between {sourceId} and {targetId}.", projectId);
    }

    public async Task UpdateProjectRelationCheckpointAsync(int projectId, int relationId, int? checkpointId)
    {
        await RequireManageAsync(projectId);
        var relation = await db.ProjectRelations.FirstOrDefaultAsync(x => x.Id == relationId
            && x.SourceProjectId == projectId && x.Type == ProjectRelationType.Dependency)
            ?? throw new InvalidOperationException("Bağımlılık bulunamadı.");
        if (checkpointId.HasValue && !await db.PipelineCheckpoints.AnyAsync(x => x.Id == checkpointId
            && x.PipelineStage.ProjectId == projectId))
            throw new InvalidOperationException("Seçilen checkpoint bu projeye ait değil.");
        relation.BlockingCheckpointId = checkpointId;
        await db.SaveChangesAsync();
        await audit.LogAsync("ProjectDependencyCheckpointChanged", "ProjectRelation", relation.Id,
            (await access.ActorAsync()).UserId, $"Project dependency checkpoint changed to {checkpointId?.ToString() ?? "project level"}.", projectId);
    }

    public async Task RemoveProjectRelationAsync(int projectId, int relationId)
    {
        await RequireManageAsync(projectId);
        var relation = await db.ProjectRelations.FirstOrDefaultAsync(x => x.Id == relationId
            && (x.SourceProjectId == projectId || x.TargetProjectId == projectId))
            ?? throw new InvalidOperationException("Proje bağlantısı bulunamadı.");
        db.ProjectRelations.Remove(relation);
        await db.SaveChangesAsync();
        await audit.LogAsync("ProjectRelationRemoved", "ProjectRelation", relation.Id, (await access.ActorAsync()).UserId,
            $"Project relation {relation.Type} removed between {relation.SourceProjectId} and {relation.TargetProjectId}.", projectId);
    }

    private async Task<bool> CreatesDependencyCycleAsync(int sourceId, int targetId)
    {
        var edges = await db.ProjectRelations.AsNoTracking()
            .Where(x => x.Type == ProjectRelationType.Dependency)
            .Select(x => new { x.SourceProjectId, x.TargetProjectId }).ToListAsync();
        var next = edges.GroupBy(x => x.SourceProjectId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.TargetProjectId).ToArray());
        var pending = new Stack<int>();
        var visited = new HashSet<int>();
        pending.Push(targetId);
        while (pending.TryPop(out var current))
        {
            if (current == sourceId) return true;
            if (!visited.Add(current) || !next.TryGetValue(current, out var targets)) continue;
            foreach (var target in targets) pending.Push(target);
        }
        return false;
    }

    public async Task AddStageAsync(int projectId, string name, string? description)
    {
        await RequireManageAsync(projectId);
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Aşama adı zorunludur.");
        var nextOrder = (await db.PipelineStages.Where(x => x.ProjectId == projectId)
            .MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1;
        var stage = new PipelineStage { ProjectId = projectId, Name = name.Trim(), Description = description?.Trim(), SortOrder = nextOrder };
        db.PipelineStages.Add(stage);
        await db.SaveChangesAsync();
        await audit.LogAsync("PipelineStageCreated", "PipelineStage", stage.Id, (await access.ActorAsync()).UserId,
            $"Pipeline stage '{stage.Name}' created for project {projectId}.", projectId);
    }

    public async Task UpdateStageAsync(int projectId, int stageId, string name, string? description)
    {
        await RequireManageAsync(projectId);
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Aşama adı zorunludur.");
        var stage = await db.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId && x.ProjectId == projectId)
            ?? throw new InvalidOperationException("Aşama bulunamadı.");
        stage.Name = name.Trim();
        stage.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        await db.SaveChangesAsync();
        await audit.LogAsync("PipelineStageUpdated", "PipelineStage", stage.Id, (await access.ActorAsync()).UserId,
            $"Pipeline stage '{stage.Name}' updated for project {projectId}.", projectId);
    }

    public async Task MoveStageAsync(int projectId, int stageId, int direction)
    {
        await RequireManageAsync(projectId);
        if (direction is not (-1 or 1)) throw new InvalidOperationException("Geçersiz sıralama yönü.");
        var stages = await db.PipelineStages.Where(x => x.ProjectId == projectId).OrderBy(x => x.SortOrder).ToListAsync();
        var index = stages.FindIndex(x => x.Id == stageId);
        if (index < 0) throw new InvalidOperationException("Aşama bulunamadı.");
        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= stages.Count) return;
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SwapOrdersAsync(stages[index], stages[targetIndex]);
        await transaction.CommitAsync();
        await audit.LogAsync("PipelineStageMoved", "PipelineStage", stageId, (await access.ActorAsync()).UserId,
            $"Pipeline stage moved to position {targetIndex + 1} in project {projectId}.", projectId);
    }

    public async Task DeleteStageAsync(int projectId, int stageId)
    {
        await RequireManageAsync(projectId);
        var stage = await db.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId && x.ProjectId == projectId)
            ?? throw new InvalidOperationException("Aşama bulunamadı.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var tasks = await db.TaskItems.Where(x => x.ProjectId == projectId && x.PipelineStageId == stageId).ToListAsync();
        var checkpointIds = await db.PipelineCheckpoints.Where(x => x.PipelineStageId == stageId).Select(x => x.Id).ToListAsync();
        var dependencies = await db.ProjectRelations.Where(x => x.BlockingCheckpointId.HasValue
            && checkpointIds.Contains(x.BlockingCheckpointId.Value)).ToListAsync();
        foreach (var dependency in dependencies) dependency.BlockingCheckpointId = null;
        foreach (var task in tasks) { task.PipelineStageId = null; task.PipelineCheckpointId = null; }
        db.PipelineStages.Remove(stage);
        await db.SaveChangesAsync();
        await NormalizeStageOrdersAsync(projectId);
        await transaction.CommitAsync();
        await audit.LogAsync("PipelineStageDeleted", "PipelineStage", stageId, (await access.ActorAsync()).UserId,
            $"Pipeline stage '{stage.Name}' deleted; {tasks.Count} tasks returned to the waiting area.", projectId);
    }

    public async Task AddCheckpointAsync(int projectId, int stageId, string name, bool requiresApproval, string? description = null)
    {
        await RequireManageAsync(projectId);
        var stage = await db.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId && x.ProjectId == projectId)
            ?? throw new InvalidOperationException("Aşama bulunamadı.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Checkpoint adı zorunludur.");
        var nextOrder = (await db.PipelineCheckpoints.Where(x => x.PipelineStageId == stageId)
            .MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1;
        var checkpoint = new PipelineCheckpoint { PipelineStageId = stage.Id, Name = name.Trim(), Description = description?.Trim(), SortOrder = nextOrder,
            RequiresApproval = requiresApproval };
        db.PipelineCheckpoints.Add(checkpoint);
        await db.SaveChangesAsync();
        await audit.LogAsync("PipelineCheckpointCreated", "PipelineCheckpoint", checkpoint.Id, (await access.ActorAsync()).UserId,
            $"Pipeline checkpoint '{checkpoint.Name}' created.", projectId);
    }

    public async Task UpdateCheckpointAsync(int projectId, int checkpointId, string name, string? description, bool requiresApproval)
    {
        await RequireManageAsync(projectId);
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Checkpoint adı zorunludur.");
        var checkpoint = await db.PipelineCheckpoints.Include(x => x.PipelineStage)
            .FirstOrDefaultAsync(x => x.Id == checkpointId && x.PipelineStage.ProjectId == projectId)
            ?? throw new InvalidOperationException("Checkpoint bulunamadı.");
        checkpoint.Name = name.Trim();
        checkpoint.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        checkpoint.RequiresApproval = requiresApproval;
        if (!requiresApproval) { checkpoint.ApprovedAt = null; checkpoint.ApprovedByUserId = null; }
        await db.SaveChangesAsync();
        await audit.LogAsync("PipelineCheckpointUpdated", "PipelineCheckpoint", checkpoint.Id, (await access.ActorAsync()).UserId,
            $"Pipeline checkpoint '{checkpoint.Name}' updated.", projectId);
    }

    public async Task MoveCheckpointAsync(int projectId, int checkpointId, int direction)
    {
        await RequireManageAsync(projectId);
        if (direction is not (-1 or 1)) throw new InvalidOperationException("Geçersiz sıralama yönü.");
        var checkpoint = await db.PipelineCheckpoints.Include(x => x.PipelineStage)
            .FirstOrDefaultAsync(x => x.Id == checkpointId && x.PipelineStage.ProjectId == projectId)
            ?? throw new InvalidOperationException("Checkpoint bulunamadı.");
        var checkpoints = await db.PipelineCheckpoints.Where(x => x.PipelineStageId == checkpoint.PipelineStageId)
            .OrderBy(x => x.SortOrder).ToListAsync();
        var index = checkpoints.FindIndex(x => x.Id == checkpointId);
        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= checkpoints.Count) return;
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SwapOrdersAsync(checkpoints[index], checkpoints[targetIndex]);
        await transaction.CommitAsync();
        await audit.LogAsync("PipelineCheckpointMoved", "PipelineCheckpoint", checkpointId, (await access.ActorAsync()).UserId,
            $"Pipeline checkpoint moved to position {targetIndex + 1}.", projectId);
    }

    public async Task DeleteCheckpointAsync(int projectId, int checkpointId)
    {
        await RequireManageAsync(projectId);
        var checkpoint = await db.PipelineCheckpoints.Include(x => x.PipelineStage)
            .FirstOrDefaultAsync(x => x.Id == checkpointId && x.PipelineStage.ProjectId == projectId)
            ?? throw new InvalidOperationException("Checkpoint bulunamadı.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var tasks = await db.TaskItems.Where(x => x.ProjectId == projectId && x.PipelineCheckpointId == checkpointId).ToListAsync();
        var dependencies = await db.ProjectRelations.Where(x => x.BlockingCheckpointId == checkpointId).ToListAsync();
        foreach (var dependency in dependencies) dependency.BlockingCheckpointId = null;
        foreach (var task in tasks) task.PipelineCheckpointId = null;
        db.PipelineCheckpoints.Remove(checkpoint);
        await db.SaveChangesAsync();
        await NormalizeCheckpointOrdersAsync(checkpoint.PipelineStageId);
        await transaction.CommitAsync();
        await audit.LogAsync("PipelineCheckpointDeleted", "PipelineCheckpoint", checkpointId, (await access.ActorAsync()).UserId,
            $"Pipeline checkpoint '{checkpoint.Name}' deleted; {tasks.Count} tasks kept in their stage.", projectId);
    }

    public async Task AssignTaskAsync(int projectId, int taskId, int? stageId, int? checkpointId)
    {
        await RequireManageAsync(projectId);
        AccessService.Require((await access.TaskAsync(taskId)).Edit);
        var task = await db.TaskItems.FirstOrDefaultAsync(x => x.Id == taskId && x.ProjectId == projectId)
            ?? throw new InvalidOperationException("Görev bulunamadı.");
        PipelineStage? stage = null;
        if (stageId.HasValue)
            stage = await db.PipelineStages.FirstOrDefaultAsync(x => x.Id == stageId && x.ProjectId == projectId)
                ?? throw new InvalidOperationException("Aşama bulunamadı.");
        if (checkpointId.HasValue)
        {
            if (stage is null || !await db.PipelineCheckpoints.AnyAsync(x => x.Id == checkpointId && x.PipelineStageId == stage.Id))
                throw new InvalidOperationException("Checkpoint seçilen aşamaya ait değil.");
        }
        task.PipelineStageId = stage?.Id;
        task.PipelineCheckpointId = checkpointId;
        await db.SaveChangesAsync();
        await audit.LogAsync("PipelineTaskAssigned", "TaskItem", task.Id, (await access.ActorAsync()).UserId,
            $"Task '{task.Title}' pipeline assignment changed.", projectId);
    }

    public async Task UpdateTaskScheduleAsync(int projectId, int taskId, DateTime? plannedStartDate, DateTime? plannedEndDate)
    {
        await RequireManageAsync(projectId);
        AccessService.Require((await access.TaskAsync(taskId)).Edit);
        if (plannedStartDate.HasValue && plannedEndDate.HasValue && plannedEndDate.Value.Date < plannedStartDate.Value.Date)
            throw new InvalidOperationException("Planlanan bitiş tarihi başlangıç tarihinden önce olamaz.");
        var task = await db.TaskItems.FirstOrDefaultAsync(x => x.Id == taskId && x.ProjectId == projectId)
            ?? throw new InvalidOperationException("Görev bulunamadı.");
        task.PlannedStartDate = plannedStartDate?.Date;
        task.DueDate = plannedEndDate?.Date;
        await db.SaveChangesAsync();
        await audit.LogAsync("TaskScheduleChanged", "TaskItem", task.Id, (await access.ActorAsync()).UserId,
            $"Task '{task.Title}' schedule changed to {task.PlannedStartDate:yyyy-MM-dd} / {task.DueDate:yyyy-MM-dd}.", projectId);
    }

    public async Task ApproveCheckpointAsync(int projectId, int checkpointId)
    {
        await RequireManageAsync(projectId);
        var checkpoint = await db.PipelineCheckpoints.Include(x => x.PipelineStage).Include(x => x.Tasks)
            .Include(x => x.BlockingProjectRelations).ThenInclude(x => x.TargetProject)
            .FirstOrDefaultAsync(x => x.Id == checkpointId && x.PipelineStage.ProjectId == projectId)
            ?? throw new InvalidOperationException("Checkpoint bulunamadı.");
        if (!checkpoint.RequiresApproval) throw new InvalidOperationException("Bu checkpoint ayrıca onay gerektirmiyor.");
        var activeTasks = checkpoint.Tasks.Where(x => !x.IsDeleted).ToList();
        if (activeTasks.Count == 0 || activeTasks.Any(x => x.Status != Models.TaskStatus.Done))
            throw new InvalidOperationException("Checkpoint onayı için bağlı görevlerin tamamlanması gerekir.");
        if (checkpoint.BlockingProjectRelations.Any(x => x.TargetProject.Status != ProjectStatus.Completed))
            throw new InvalidOperationException("Checkpoint onayı için bağlı projelerin tamamlanması gerekir.");
        checkpoint.ApprovedAt = DateTime.UtcNow;
        checkpoint.ApprovedByUserId = (await access.ActorAsync()).UserId;
        await db.SaveChangesAsync();
        await audit.LogAsync("PipelineCheckpointApproved", "PipelineCheckpoint", checkpoint.Id, checkpoint.ApprovedByUserId,
            $"Pipeline checkpoint '{checkpoint.Name}' approved.", projectId);
    }

    private async Task RequireManageAsync(int projectId)
    {
        var rights = await access.ProjectAsync(projectId);
        AccessService.Require(rights.ChangeStatus || (await access.ActorAsync()).IsAdmin);
    }

    private async Task SwapOrdersAsync(PipelineStage first, PipelineStage second)
    {
        var firstOrder = first.SortOrder;
        var secondOrder = second.SortOrder;
        first.SortOrder = 0;
        await db.SaveChangesAsync();
        second.SortOrder = firstOrder;
        await db.SaveChangesAsync();
        first.SortOrder = secondOrder;
        await db.SaveChangesAsync();
    }

    private async Task SwapOrdersAsync(PipelineCheckpoint first, PipelineCheckpoint second)
    {
        var firstOrder = first.SortOrder;
        var secondOrder = second.SortOrder;
        first.SortOrder = 0;
        await db.SaveChangesAsync();
        second.SortOrder = firstOrder;
        await db.SaveChangesAsync();
        first.SortOrder = secondOrder;
        await db.SaveChangesAsync();
    }

    private async Task NormalizeStageOrdersAsync(int projectId)
    {
        var stages = await db.PipelineStages.Where(x => x.ProjectId == projectId).OrderBy(x => x.SortOrder).ToListAsync();
        for (var i = 0; i < stages.Count; i++) stages[i].SortOrder = -(i + 1);
        await db.SaveChangesAsync();
        for (var i = 0; i < stages.Count; i++) stages[i].SortOrder = i + 1;
        await db.SaveChangesAsync();
    }

    private async Task NormalizeCheckpointOrdersAsync(int stageId)
    {
        var checkpoints = await db.PipelineCheckpoints.Where(x => x.PipelineStageId == stageId).OrderBy(x => x.SortOrder).ToListAsync();
        for (var i = 0; i < checkpoints.Count; i++) checkpoints[i].SortOrder = -(i + 1);
        await db.SaveChangesAsync();
        for (var i = 0; i < checkpoints.Count; i++) checkpoints[i].SortOrder = i + 1;
        await db.SaveChangesAsync();
    }
}
