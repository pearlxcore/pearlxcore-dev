using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace pearlxcore.dev.Services.Implementations;

public class AdminProfileService : IAdminProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminProfileService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<AdminProfile> GetAsync()
    {
        var profile = await _context.AdminProfiles.AsNoTracking().FirstOrDefaultAsync();
        if (profile != null)
        {
            return profile;
        }

        var defaultProfile = new AdminProfile
        {
            Name = "Admin",
            Title = "Site Owner",
            Bio = "",
            AvatarUrl = null,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AdminProfiles.Add(defaultProfile);
        await _context.SaveChangesAsync();
        return defaultProfile;
    }

    public async Task SaveAsync(AdminProfile profile)
    {
        var existing = await _context.AdminProfiles.FirstOrDefaultAsync();
        if (existing == null)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            _context.AdminProfiles.Add(profile);
            Log.Information("Admin profile created: {Name}", profile.Name);
        }
        else
        {
            existing.Name = profile.Name;
            existing.Title = profile.Title;
            existing.Bio = profile.Bio;
            existing.AvatarUrl = profile.AvatarUrl;
            existing.CvUrl = profile.CvUrl;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.AdminProfiles.Update(existing);
            Log.Information("Admin profile updated: {Name}", profile.Name);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<string?> SaveAvatarAsync(IFormFile? avatarFile)
    {
        if (avatarFile == null || avatarFile.Length == 0)
        {
            Log.Debug("Avatar upload attempt with no file");
            return null;
        }

        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        if (avatarFile.Length > maxFileSize)
        {
            Log.Warning("Avatar upload failed: File too large ({Size} bytes), Max: {MaxSize}", avatarFile.Length, maxFileSize);
            return null;
        }

        var ext = Path.GetExtension(avatarFile.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            Log.Warning("Avatar upload failed: Invalid extension {Extension}", ext);
            return null;
        }

        var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await avatarFile.CopyToAsync(stream);
        }

        Log.Information("Avatar uploaded successfully: {FileName} ({Size} bytes)", fileName, avatarFile.Length);
        return $"/images/avatars/{fileName}";
    }

    public async Task<string?> SaveCvAsync(IFormFile? cvFile)
    {
        if (cvFile == null || cvFile.Length == 0)
        {
            Log.Debug("CV upload attempt with no file");
            return null;
        }

        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

        if (cvFile.Length > maxFileSize)
        {
            Log.Warning("CV upload failed: File too large ({Size} bytes), Max: {MaxSize}", cvFile.Length, maxFileSize);
            return null;
        }

        var ext = Path.GetExtension(cvFile.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            Log.Warning("CV upload failed: Invalid extension {Extension}", ext);
            return null;
        }

        var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "files", "cv");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"resume_{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await cvFile.CopyToAsync(stream);
        }

        Log.Information("CV uploaded successfully: {FileName} ({Size} bytes)", fileName, cvFile.Length);
        return $"/files/cv/{fileName}";
    }
}
