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
        "PS2",
        "PS3",
        "PS4",
        "PS5",
        "Misc"
    ];

    private static readonly string[] ProjectPlatforms =
    [
        "Desktop",
        "Web App",
        "Mobile",
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

    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;

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
        var screenshotValidationError = ValidateImageFile(model.ScreenshotFile);
        if (screenshotValidationError != null)
        {
            ModelState.AddModelError(nameof(model.ScreenshotFile), screenshotValidationError);
        }

        if (!ModelState.IsValid)
        {
            PopulateOptions();
            return View(model);
        }

        var screenshotUrl = await _projectService.SaveProjectScreenshotAsync(model.ScreenshotFile);
        var finalScreenshotUrl = screenshotUrl ?? model.ScreenshotUrl;

        var project = new Project
        {
            Title = model.Title,
            ProjectType = model.ProjectType,
            Platform = model.Platform,
            Status = model.Status,
            Description = model.Description,
            GitHubUrl = model.GitHubUrl,
            ScreenshotUrl = finalScreenshotUrl,
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
            Platform = project.Platform,
            Status = project.Status,
            Description = project.Description,
            GitHubUrl = project.GitHubUrl,
            ScreenshotUrl = project.ScreenshotUrl,
            SortOrder = project.SortOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectFormViewModel model)
    {
        var screenshotValidationError = ValidateImageFile(model.ScreenshotFile);
        if (screenshotValidationError != null)
        {
            ModelState.AddModelError(nameof(model.ScreenshotFile), screenshotValidationError);
        }

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

        var screenshotUrl = await _projectService.SaveProjectScreenshotAsync(model.ScreenshotFile);

        var project = new Project
        {
            Id = existing.Id,
            Title = model.Title,
            ProjectType = model.ProjectType,
            Platform = model.Platform,
            Status = model.Status,
            Description = model.Description,
            GitHubUrl = model.GitHubUrl,
            ScreenshotUrl = screenshotUrl ?? existing.ScreenshotUrl,
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
        ViewBag.ProjectPlatformOptions = ProjectPlatforms.Select(x => new SelectListItem(x, x)).ToList();
        ViewBag.ProjectStatusOptions = ProjectStatuses.Select(x => new SelectListItem(x, x)).ToList();
    }

    private static string? ValidateImageFile(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return null;
        }

        if (imageFile.Length > MaxImageSizeBytes)
        {
            return "Screenshot is too large. Maximum size is 5 MB.";
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            return "Unsupported image format. Allowed: JPG, JPEG, PNG, GIF, WEBP.";
        }

        return null;
    }
}
