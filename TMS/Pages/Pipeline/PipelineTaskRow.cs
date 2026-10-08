using TMS.Models;
using TMS.Services;

namespace TMS.Pages.Pipeline;

public record PipelineTaskRow(TaskItem Task, ProjectPipelineView Pipeline, int? CurrentStageId);
