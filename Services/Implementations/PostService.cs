using pearlxcore.dev.Data;
using pearlxcore.dev.Models;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Markdig;
using Serilog;
using Ganss.Xss;
using Microsoft.Extensions.FileProviders;

namespace pearlxcore.dev.Services.Implementations;

public class PostService : IPostService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PostService(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
    {
        _db = db;
        _webHostEnvironment = webHostEnvironment;
    }

    public PostService(ApplicationDbContext db)
        : this(db, new TestWebHostEnvironment())
    {
    }

    public async Task<IEnumerable<Post>> GetAllAsync()
    {
        return await _db.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaginatedList<Post>> GetAllPaginatedAsync(int pageIndex = 1, int pageSize = 10)
    {
        var query = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .OrderByDescending(p => p.CreatedAt);

        var count = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<Post>(items, count, pageIndex, pageSize);
    }

    public async Task<Post?> GetByIdAsync(int id)
    {
        return await _db.Posts
            .Include(p => p.PostCategories)
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

    }

    public async Task CreateAsync(
    Post post,
    IEnumerable<int> categoryIds,
    IEnumerable<int> tagIds)
    {
        if (string.IsNullOrWhiteSpace(post.Content))
        {
            Log.Warning("Attempted to create post with empty content");
            throw new InvalidOperationException("Post content cannot be empty.");
        }

        var baseSlug = string.IsNullOrWhiteSpace(post.Slug)
            ? GenerateSlugFromTitle(post.Title)
            : NormalizeSlug(post.Slug);

        post.Slug = await EnsureUniqueSlugAsync(baseSlug);

        var rawHtml = Markdown.ToHtml(post.Content, _markdownPipeline);
        post.RenderedContent = _htmlSanitizer.Sanitize(rawHtml);
        
        Log.Debug("Rendered and sanitized post content for: {Title}", post.Title);

        UpdateCategories(post, categoryIds);
        UpdateTags(post, tagIds);

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        
        Log.Information("Post created: {Title} (ID: {PostId}, Slug: {Slug})", post.Title, post.Id, post.Slug);
    }

    public async Task UpdateAsync(
    Post post,
    IEnumerable<int> categoryIds,
    IEnumerable<int> tagIds)
    {
        var baseSlug = NormalizeSlug(post.Slug);
        post.Slug = await EnsureUniqueSlugAsync(baseSlug, post.Id);

        var rawHtml = Markdown.ToHtml(post.Content, _markdownPipeline);
        post.RenderedContent = _htmlSanitizer.Sanitize(rawHtml);
        post.UpdatedAt = DateTime.UtcNow;
        
        Log.Debug("Rendered and sanitized updated post content for: {Title}", post.Title);

        UpdateCategories(post, categoryIds);
        UpdateTags(post, tagIds);

        _db.Posts.Update(post);
        await _db.SaveChangesAsync();
        
        Log.Information("Post updated: {Title} (ID: {PostId}, Slug: {Slug})", post.Title, post.Id, post.Slug);
    }

    public async Task DeleteAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post != null)
        {
            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
            Log.Information("Post deleted: {Title} (ID: {PostId})", post.Title, post.Id);
        }
        else
        {
            Log.Warning("Attempted to delete non-existent post with ID: {PostId}", id);
        }
    }

    public async Task<bool> SlugExistsAsync(string slug, int? ignorePostId = null)
    {
        return await _db.Posts.AnyAsync(p =>
            p.Slug == slug &&
            (!ignorePostId.HasValue || p.Id != ignorePostId.Value));
    }


    public async Task<IEnumerable<Post>> GetPublishedAsync()
    {
        return await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();
    }

    public async Task<PaginatedList<Post>> GetPublishedPaginatedAsync(int pageIndex = 1, int pageSize = 10)
    {
        var query = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt);

        var count = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<Post>(items, count, pageIndex, pageSize);
    }

    public async Task<Post?> GetPublishedBySlugAsync(string slug)
    {
        return await _db.Posts
             .Include(p => p.Author)
             .Include(p => p.PostCategories)
                 .ThenInclude(pc => pc.Category)
             .Include(p => p.PostTags)
                 .ThenInclude(pt => pt.Tag)
             .FirstOrDefaultAsync(p => p.IsPublished && p.Slug == slug);

    }

    private static string NormalizeSlug(string? slug)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? string.Empty
            : slug.Trim().ToLowerInvariant();
    }


    private static string GenerateSlugFromTitle(string title)
    {
        var slug = title.ToLowerInvariant().Trim();

        // Replace invalid chars with hyphen
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

        // Convert multiple spaces into one hyphen
        slug = Regex.Replace(slug, @"\s+", "-");

        // Trim hyphens
        slug = slug.Trim('-');

        return slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? ignorePostId = null)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await SlugExistsAsync(slug, ignorePostId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private static readonly MarkdownPipeline _markdownPipeline =
    new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly HtmlSanitizer _htmlSanitizer = new HtmlSanitizer();

    static PostService()
    {
        // Configure allowed tags and attributes for safe rendering
        foreach (var tag in new[] { "div", "section", "pre", "code", "blockquote", "p", "br", "hr", "ul", "ol", "li", "span", "strong", "em", "a", "img", "h1", "h2", "h3", "h4", "h5", "h6" })
        {
            _htmlSanitizer.AllowedTags.Add(tag);
        }
        _htmlSanitizer.AllowedAttributes.Add("class");
        _htmlSanitizer.AllowedAttributes.Add("id");
        _htmlSanitizer.AllowedAttributes.Add("style");
        _htmlSanitizer.AllowedAttributes.Add("src");
        _htmlSanitizer.AllowedAttributes.Add("alt");
        _htmlSanitizer.AllowedAttributes.Add("href");
        _htmlSanitizer.AllowedAttributes.Add("title");
        _htmlSanitizer.AllowedCssProperties.Add("color");
        _htmlSanitizer.AllowedCssProperties.Add("background-color");
        _htmlSanitizer.AllowedCssProperties.Add("text-align");
        _htmlSanitizer.AllowedCssProperties.Add("max-width");
        _htmlSanitizer.AllowedCssProperties.Add("width");
        _htmlSanitizer.AllowedCssProperties.Add("height");
        _htmlSanitizer.AllowedCssProperties.Add("display");
        _htmlSanitizer.AllowedCssProperties.Add("margin");
    }

    private static void UpdateCategories(Post post, IEnumerable<int> categoryIds)
    {
        post.PostCategories.Clear();

        foreach (var id in categoryIds)
        {
            post.PostCategories.Add(new PostCategory
            {
                CategoryId = id
            });
        }
    }

    private static void UpdateTags(Post post, IEnumerable<int> tagIds)
    {
        post.PostTags.Clear();

        foreach (var id in tagIds)
        {
            post.PostTags.Add(new PostTag
            {
                TagId = id
            });
        }
    }

    public async Task<IEnumerable<Post>> GetPublishedByCategoryAsync(string categorySlug)
    {
        return await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
            .Where(p =>
                p.IsPublished &&
                p.PostCategories.Any(pc => pc.Category.Slug == categorySlug))
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();
    }

    public async Task<PaginatedList<Post>> GetPublishedByCategoryPaginatedAsync(string categorySlug, int pageIndex = 1, int pageSize = 10)
    {
        var query = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
            .Where(p =>
                p.IsPublished &&
                p.PostCategories.Any(pc => pc.Category.Slug == categorySlug))
            .OrderByDescending(p => p.PublishedAt);

        var count = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<Post>(items, count, pageIndex, pageSize);
    }

    public async Task<IEnumerable<Post>> GetPublishedByTagAsync(string tagSlug)
    {
        return await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p =>
                p.IsPublished &&
                p.PostTags.Any(pt => pt.Tag.Slug == tagSlug))
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();
    }

    public async Task<PaginatedList<Post>> GetPublishedByTagPaginatedAsync(string tagSlug, int pageIndex = 1, int pageSize = 10)
    {
        var query = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p =>
                p.IsPublished &&
                p.PostTags.Any(pt => pt.Tag.Slug == tagSlug))
            .OrderByDescending(p => p.PublishedAt);

        var count = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<Post>(items, count, pageIndex, pageSize);
    }

    public async Task<PaginatedList<Post>> SearchPublishedAsync(string query, int pageIndex = 1, int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // If no query, return all published posts
            return await GetPublishedPaginatedAsync(pageIndex, pageSize);
        }

        var searchTerm = query.Trim().ToLower();
        
        var postsQuery = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.IsPublished && 
                (p.Title.ToLower().Contains(searchTerm) ||
                 p.Content.ToLower().Contains(searchTerm) ||
                 (p.Summary != null && p.Summary.ToLower().Contains(searchTerm)) ||
                 p.PostCategories.Any(pc => pc.Category.Name.ToLower().Contains(searchTerm)) ||
                 p.PostTags.Any(pt => pt.Tag.Name.ToLower().Contains(searchTerm))))
            .OrderByDescending(p => p.PublishedAt);

        var count = await postsQuery.CountAsync();
        var items = await postsQuery
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Log.Information("Search query '{Query}' returned {Count} results", query, count);

        return new PaginatedList<Post>(items, count, pageIndex, pageSize);
    }

    public async Task<string?> SavePostImageAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return null;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

        if (!allowedExtensions.Contains(fileExtension))
            return null;

        // Maximum 5MB
        if (imageFile.Length > 5 * 1024 * 1024)
            return null;

        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var uploadPath = GetPostImageUploadPath();

        try
        {
            if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
            {
                Log.Error("WebRootPath is not configured or empty");
                return null;
            }

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
                Log.Information("Created directory: {UploadPath}", uploadPath);
            }

            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            Log.Information("Image uploaded successfully: {FileName} to {FilePath}", fileName, filePath);
            return $"/images/posts/{fileName}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error uploading image: {FileName}", fileName);
            return null;
        }
    }

    private string GetPostImageUploadPath()
    {
        return Path.GetFullPath(Path.Combine(_webHostEnvironment.ContentRootPath, "..", "..", "shared", "uploads", "posts"));
    }

    // Dashboard Statistics
    public async Task<int> GetTotalCountAsync()
    {
        return await _db.Posts.CountAsync();
    }

    public async Task<int> GetPublishedCountAsync()
    {
        return await _db.Posts.CountAsync(p => p.IsPublished);
    }

    public async Task<int> GetDraftCountAsync()
    {
        return await _db.Posts.CountAsync(p => !p.IsPublished && (p.ScheduledPublishAt == null || p.ScheduledPublishAt > DateTime.UtcNow));
    }

    public async Task<int> GetScheduledCountAsync()
    {
        return await _db.Posts.CountAsync(p => !p.IsPublished && p.ScheduledPublishAt != null && p.ScheduledPublishAt > DateTime.UtcNow);
    }

    public async Task<List<Post>> GetRecentPostsAsync(int count = 5)
    {
        return await _db.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Post>> GetRecentDraftsAsync(int count = 5)
    {
        return await _db.Posts
            .Include(p => p.Author)
            .Where(p => !p.IsPublished && (p.ScheduledPublishAt == null || p.ScheduledPublishAt > DateTime.UtcNow))
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Post>> GetScheduledPostsAsync(int count = 5)
    {
        return await _db.Posts
            .Include(p => p.Author)
            .Where(p => !p.IsPublished && p.ScheduledPublishAt != null && p.ScheduledPublishAt > DateTime.UtcNow)
            .OrderBy(p => p.ScheduledPublishAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Post?> GetLastPublishedPostAsync()
    {
        return await _db.Posts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Post>> GetRelatedPostsAsync(int postId, int limit = 3)
    {
        // Get the current post with its categories and tags
        var currentPost = await _db.Posts
            .Include(p => p.PostCategories)
            .ThenInclude(pc => pc.Category)
            .Include(p => p.PostTags)
            .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (currentPost == null)
            return new List<Post>();

        var categoryIds = currentPost.PostCategories.Select(pc => pc.CategoryId).ToList();
        var tagIds = currentPost.PostTags.Select(pt => pt.TagId).ToList();

        // Get published posts that share categories or tags, excluding the current post
        var relatedPosts = await _db.Posts
            .Include(p => p.Author)
            .Where(p => p.Id != postId && p.IsPublished && (
                p.PostCategories.Any(pc => categoryIds.Contains(pc.CategoryId)) ||
                p.PostTags.Any(pt => tagIds.Contains(pt.TagId))
            ))
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit)
            .ToListAsync();

        return relatedPosts;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "pearlxcore.dev.Tests";
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "pearlxcore-webroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

}

