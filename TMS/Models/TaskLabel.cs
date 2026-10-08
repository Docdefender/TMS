using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class TaskLabel
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string NormalizedName { get; set; } = string.Empty;

    [Required, StringLength(7)]
    public string Color { get; set; } = "#64748B";

    public ICollection<TaskItemLabel> Tasks { get; set; } = new List<TaskItemLabel>();
}

public class TaskItemLabel
{
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public int TaskLabelId { get; set; }
    public TaskLabel TaskLabel { get; set; } = null!;
}
