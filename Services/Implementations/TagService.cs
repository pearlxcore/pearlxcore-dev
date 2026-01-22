using pearlxcore.dev.Data;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace pearlxcore.dev.Services.Implementations;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _db;

    public TagService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
        => await _db.Tags.OrderBy(t => t.Name).ToListAsync();

    public async Task<Tag?> GetByIdAsync(int id)
        => await _db.Tags.FindAsync(id);

    public async Task CreateAsync(Tag tag)
    {
        // Ensure unique slug
        var baseSlug = NormalizeSlug(tag.Slug ?? tag.Name);
        tag.Slug = await EnsureUniqueSlugAsync(baseSlug);
        
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag != null)
        {
            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync();
        }
    }

    private static string NormalizeSlug(string slug)
    {
        // Convert to lowercase
        slug = slug.ToLower();
        
        // Replace spaces and underscores with hyphens
        slug = Regex.Replace(slug, @"[\s_]+", "-");
        
        // Remove special characters, keep only alphanumeric and hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        
        // Remove consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");
        
        // Remove leading/trailing hyphens
        slug = slug.Trim('-');
        
        return slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? ignoreTagId = null)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await SlugExistsAsync(slug, ignoreTagId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private async Task<bool> SlugExistsAsync(string slug, int? ignoreTagId = null)
    {
        return await _db.Tags.AnyAsync(t =>
            t.Slug == slug &&
            (!ignoreTagId.HasValue || t.Id != ignoreTagId.Value));
    }
}
