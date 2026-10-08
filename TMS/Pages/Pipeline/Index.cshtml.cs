using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TMS.Services;

namespace TMS.Pages.Pipeline;

[Authorize]
public class IndexModel(PipelineService pipeline) : PageModel
{
    public List<PipelineProjectSummary> Projects { get; private set; } = new();

    public async Task OnGetAsync() => Projects = await pipeline.GetProjectsAsync();
}
