using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace pearlxcore.dev.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _db;

    public ProjectService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
        => await _db.Projects
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .ToListAsync();

    public async Task<Project?> GetByIdAsync(int id)
        => await _db.Projects.FindAsync(id);

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
        existing.Status = project.Status;
        existing.Summary = project.Summary;
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
