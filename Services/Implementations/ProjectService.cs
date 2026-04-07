using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Serilog;

namespace pearlxcore.dev.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProjectService(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
    {
        _db = db;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
        => await _db.Projects
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .ToListAsync();

    public async Task<Project?> GetByIdAsync(int id)
        => await _db.Projects.FindAsync(id);

    public async Task<string?> SaveProjectScreenshotAsync(IFormFile? screenshotFile)
    {
        if (screenshotFile == null || screenshotFile.Length == 0)
        {
            return null;
        }

        const long maxFileSize = 5 * 1024 * 1024;
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        if (screenshotFile.Length > maxFileSize)
        {
            Log.Warning("Project screenshot upload failed: File too large ({Size} bytes)", screenshotFile.Length);
            return null;
        }

        var ext = Path.GetExtension(screenshotFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            Log.Warning("Project screenshot upload failed: Invalid extension {Extension}", ext);
            return null;
        }

        var uploadsDir = Path.GetFullPath(Path.Combine(_webHostEnvironment.ContentRootPath, "..", "..", "shared", "uploads", "projects"));
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await screenshotFile.CopyToAsync(stream);
            }

            Log.Information("Project screenshot uploaded successfully: {FileName}", fileName);
            return $"/images/projects/{fileName}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Project screenshot upload failed for {FileName}", fileName);
            return null;
        }
    }

    public async Task CreateAsync(Project project)
    {
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        Log.Information("Project created: {Title}", project.Title);
    }

    public async Task UpdateAsync(Project project)
    {
        var existing = await _db.Projects.FindAsync(project.Id);
        if (existing == null)
        {
            return;
        }

        existing.Title = project.Title;
        existing.ProjectType = project.ProjectType;
        existing.Platform = project.Platform;
        existing.Status = project.Status;
        existing.Description = project.Description;
        existing.GitHubUrl = project.GitHubUrl;
        existing.DownloadUrl = project.DownloadUrl;
        existing.ScreenshotUrl = project.ScreenshotUrl;
        existing.SortOrder = project.SortOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        _db.Projects.Update(existing);
        await _db.SaveChangesAsync();
        Log.Information("Project updated: {Title}", project.Title);
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null)
        {
            return;
        }

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        Log.Information("Project deleted: {Title}", project.Title);
    }
}
