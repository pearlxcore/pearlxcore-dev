using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.ViewModels.Admin.Projects;
using Serilog;

namespace pearlxcore.dev.Areas.Admin.Controllers;

public class ProjectsController : AdminController
{
    private static readonly string[] ProjectTypes =
    [
        "Desktop App",
        "PS4 / PS5 Community",
        "Mobile / Cellular",
        "Automation",
        "Other"
    ];

    private static readonly string[] ProjectStatuses =
    [
        "Archived",
        "Inactive",
        "Experimental",
        "Active"
    ];

    private readonly IProjectService _projectService;
    private readonly IAuditLogService _auditLogService;

    public ProjectsController(IProjectService projectService, IAuditLogService auditLogService)
    {
        _projectService = projectService;
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();
        return View(projects);
    }

    [HttpGet]
    public IActionResult Create()
    {
        PopulateOptions();
        return View(new ProjectFormViewModel
        {
            ProjectType = ProjectTypes[0],
            Status = ProjectStatuses[0]
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateOptions();
            return View(model);
        }

        var project = new Project
        {
            Title = model.Title,
            ProjectType = model.ProjectType,
            Status = model.Status,
            Summary = model.Summary,
            Description = model.Description,
            GitHubUrl = model.GitHubUrl,
            DownloadUrl = model.DownloadUrl,
            ScreenshotUrl = model.ScreenshotUrl,
            SortOrder = model.SortOrder
        };

        await _projectService.CreateAsync(project);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditLogService.LogAsync("Create", "Project", project.Id, $"Created project: {project.Title}", userId);
        Log.Information("Project created by {User}: ID={ProjectId}, Title={Title}", User.Identity?.Name, project.Id, project.Title);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        PopulateOptions();
        return View(new ProjectFormViewModel
        {
            Id = project.Id,
            Title = project.Title,
            ProjectType = project.ProjectType,
            Status = project.Status,
            Summary = project.Summary,
            Description = project.Description,
            GitHubUrl = project.GitHubUrl,
            DownloadUrl = project.DownloadUrl,
            ScreenshotUrl = project.ScreenshotUrl,
            SortOrder = project.SortOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateOptions();
            return View(model);
        }

        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        var existing = await _projectService.GetByIdAsync(model.Id.Value);
        if (existing == null)
        {
            return NotFound();
        }

        var project = new Project
        {
            Id = existing.Id,
            Title = model.Title,
            ProjectType = model.ProjectType,
            Status = model.Status,
            Summary = model.Summary,
            Description = model.Description,
            GitHubUrl = model.GitHubUrl,
            DownloadUrl = model.DownloadUrl,
            ScreenshotUrl = model.ScreenshotUrl,
            SortOrder = model.SortOrder,
            CreatedAt = existing.CreatedAt
        };

        await _projectService.UpdateAsync(project);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditLogService.LogAsync("Update", "Project", project.Id, $"Updated project: {project.Title}", userId);
        Log.Information("Project updated by {User}: ID={ProjectId}, Title={Title}", User.Identity?.Name, project.Id, project.Title);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        await _projectService.DeleteAsync(id);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditLogService.LogAsync("Delete", "Project", id, $"Deleted project: {project.Title}", userId);
        Log.Information("Project deleted by {User}: ID={ProjectId}, Title={Title}", User.Identity?.Name, id, project.Title);

        return RedirectToAction(nameof(Index));
    }

    private void PopulateOptions()
    {
        ViewBag.ProjectTypeOptions = ProjectTypes.Select(x => new SelectListItem(x, x)).ToList();
        ViewBag.ProjectStatusOptions = ProjectStatuses.Select(x => new SelectListItem(x, x)).ToList();
    }
}
