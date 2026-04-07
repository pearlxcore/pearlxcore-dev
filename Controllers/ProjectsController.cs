using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using pearlxcore.dev.Infrastructure;

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

    [HttpGet("/Projects/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        ViewData["Title"] = project.Title;
        ViewData["MetaDescription"] = !string.IsNullOrWhiteSpace(project.Description)
            ? ProjectPresentationHelper.GetExcerpt(project.Description, 160)
            : $"Project details for {project.Title}.";
        ViewBag.RenderedDescription = ProjectPresentationHelper.RenderMarkdown(project.Description);
        return View(project);
    }
}
