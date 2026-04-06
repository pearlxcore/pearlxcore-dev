using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace pearlxcore.dev.Controllers;

public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("/Projects")]
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();
        ViewData["Title"] = "Projects";
        ViewData["MetaDescription"] = "Archived and active open source projects covering desktop apps, PS4/PS5 community tools, mobile tooling, and automation.";
        return View(projects);
    }
}
