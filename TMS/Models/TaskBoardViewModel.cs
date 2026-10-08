namespace TMS.Models;

public sealed class TaskBoardViewModel
{
    public IReadOnlyList<TaskItem> Tasks { get; init; } = [];
    public IReadOnlySet<int> MovableTaskIds { get; init; } = new HashSet<int>();
    public string UpdateUrl { get; init; } = string.Empty;
    public string BoardKey { get; init; } = "tasks";
    public bool ShowProjectFilter { get; init; }
    public bool ShowToolbar { get; init; } = true;
}
