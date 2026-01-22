using pearlxcore.dev.Data;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;

    public CategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
        => await _db.Categories.OrderBy(c => c.Name).ToListAsync();

    public async Task<Category?> GetByIdAsync(int id)
        => await _db.Categories.FindAsync(id);

    public async Task CreateAsync(Category category)
    {
        // Ensure unique slug
        var baseSlug = NormalizeSlug(category.Slug ?? category.Name);
        category.Slug = await EnsureUniqueSlugAsync(baseSlug);
        
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category != null)
        {
            _db.Categories.Remove(category);
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

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? ignoreCategoryId = null)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await SlugExistsAsync(slug, ignoreCategoryId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private async Task<bool> SlugExistsAsync(string slug, int? ignoreCategoryId = null)
    {
        return await _db.Categories.AnyAsync(c =>
            c.Slug == slug &&
            (!ignoreCategoryId.HasValue || c.Id != ignoreCategoryId.Value));
    }
}
