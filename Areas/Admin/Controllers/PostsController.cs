using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.ViewModels.Admin.Posts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Markdig;
using Microsoft.AspNetCore.Antiforgery;
using Ganss.Xss;
using System.IO;
using System.Linq;
using Serilog;

namespace pearlxcore.dev.Areas.Admin.Controllers;

public class PostsController : AdminController
{
    private readonly IPostService _postService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly IAuditLogService _auditLogService;

    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private const int MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB

    public PostsController(
        IPostService postService,
        ICategoryService categoryService,
        ITagService tagService,
        IAuditLogService auditLogService)
    {
        _postService = postService;
        _categoryService = categoryService;
        _tagService = tagService;
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index(int? page)
    {
        const int pageSize = 15;
        var currentPage = page ?? 1;
        
        var paginatedPosts = await _postService.GetAllPaginatedAsync(currentPage, pageSize);
        return View(paginatedPosts);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.Tags = await _tagService.GetAllAsync();

        return View(new PostFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostFormViewModel model)
    {
        var imageValidationError = ValidateImageFile(model.ImageFile);
        if (imageValidationError != null)
        {
            ModelState.AddModelError(nameof(model.ImageFile), imageValidationError);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.Tags = await _tagService.GetAllAsync();
            return View(model);
        }

        var imageUrl = await _postService.SavePostImageAsync(model.ImageFile);
        // Use model.ImageUrl as fallback if no new file was uploaded
        var finalImageUrl = imageUrl ?? model.ImageUrl;

        DateTime? scheduledPublishAt = null;
        if (!string.IsNullOrEmpty(model.ScheduledPublishDate) && !string.IsNullOrEmpty(model.ScheduledPublishTime))
        {
            var dateStr = $"{model.ScheduledPublishDate}T{model.ScheduledPublishTime}";
            if (DateTime.TryParse(dateStr, out var scheduledDate))
            {
                scheduledPublishAt = scheduledDate.ToUniversalTime();
            }
        }

        var post = new Post
        {
            Title = model.Title,
            Slug = model.Slug,
            Content = model.Content,
            Summary = model.Summary,
            ImageUrl = finalImageUrl,
            IsPublished = model.IsPublished,
            PublishedAt = model.IsPublished ? DateTime.UtcNow : null,
            ScheduledPublishAt = !model.IsPublished ? scheduledPublishAt : null,
            AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        };

        await _postService.CreateAsync(post, model.CategoryIds, model.TagIds);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditLogService.LogAsync("Create", "Post", post.Id, $"Created post: {post.Title}", userId);
        Log.Information("Post created by {User}: ID={PostId}, Title={Title}, Published={IsPublished}", User.Identity?.Name, post.Id, post.Title, model.IsPublished);
        
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _postService.GetByIdAsync(id);
        if (post == null)
            return NotFound();

        var model = new PostFormViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Content = post.Content,
            Summary = post.Summary,
            ImageUrl = post.ImageUrl,
            IsPublished = post.IsPublished,
            ScheduledPublishDate = post.ScheduledPublishAt?.ToString("yyyy-MM-dd"),
            ScheduledPublishTime = post.ScheduledPublishAt?.ToString("HH:mm"),
            CategoryIds = post.PostCategories.Select(pc => pc.CategoryId).ToList(),
            TagIds = post.PostTags.Select(pt => pt.TagId).ToList()
        };

        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.Tags = await _tagService.GetAllAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PostFormViewModel model)
    {
        var imageValidationError = ValidateImageFile(model.ImageFile);
        if (imageValidationError != null)
        {
            ModelState.AddModelError(nameof(model.ImageFile), imageValidationError);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.Tags = await _tagService.GetAllAsync();
            return View(model);
        }

        var post = await _postService.GetByIdAsync(model.Id!.Value);
        if (post == null)
            return NotFound();

        var imageUrl = await _postService.SavePostImageAsync(model.ImageFile);

        DateTime? scheduledPublishAt = null;
        if (!string.IsNullOrEmpty(model.ScheduledPublishDate) && !string.IsNullOrEmpty(model.ScheduledPublishTime))
        {
            var dateStr = $"{model.ScheduledPublishDate}T{model.ScheduledPublishTime}";
            if (DateTime.TryParse(dateStr, out var scheduledDate))
            {
                scheduledPublishAt = scheduledDate.ToUniversalTime();
            }
        }

        post.Title = model.Title;
        post.Slug = model.Slug;
        post.Content = model.Content;
        post.Summary = model.Summary;
        post.ImageUrl = model.RemoveImage ? null : (imageUrl ?? post.ImageUrl);
        post.IsPublished = model.IsPublished;
        post.ScheduledPublishAt = !model.IsPublished ? scheduledPublishAt : null;
        post.PublishedAt = model.IsPublished && post.PublishedAt == null ? DateTime.UtcNow : post.PublishedAt;

        await _postService.UpdateAsync(post, model.CategoryIds, model.TagIds);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditLogService.LogAsync("Update", "Post", post.Id, $"Updated post: {post.Title}", userId);
        Log.Information("Post updated by {User}: ID={PostId}, Title={Title}, Published={IsPublished}", User.Identity?.Name, post.Id, post.Title, model.IsPublished);
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _postService.GetByIdAsync(id);
        if (post == null)
            return NotFound();

        await _postService.DeleteAsync(id);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditLogService.LogAsync("Delete", "Post", id, $"Deleted post: {post.Title}", userId);
        Log.Information("Post deleted by {User}: ID={PostId}, Title={Title}", User.Identity?.Name, id, post.Title);
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PreviewMarkdown([FromBody] PreviewRequest request)
    {
        if (string.IsNullOrEmpty(request?.Markdown))
            return Json(new { html = "" });

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var rawHtml = Markdown.ToHtml(request.Markdown, pipeline);
        
        // Sanitize the HTML to prevent XSS attacks in preview
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Add("div");
        sanitizer.AllowedTags.Add("section");
        sanitizer.AllowedAttributes.Add("class");
        sanitizer.AllowedAttributes.Add("id");
        
        var sanitizedHtml = sanitizer.Sanitize(rawHtml);

        return Json(new { html = sanitizedHtml });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile? imageFile)
    {
        if (imageFile == null)
            return Json(new { success = false, message = "No file provided" });

        var imageUrl = await _postService.SavePostImageAsync(imageFile);

        if (imageUrl == null)
            return Json(new { success = false, message = "Invalid file format or size" });

        return Json(new { success = true, imageUrl = imageUrl });
    }

    private static string? ValidateImageFile(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return null;

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            return "Unsupported image format. Allowed: JPG, JPEG, PNG, GIF, WEBP.";
        }

        if (imageFile.Length > MaxImageSizeBytes)
        {
            return "Image is too large. Maximum size is 5MB.";
        }

        return null;
    }
}

public class PreviewRequest
{
    public string? Markdown { get; set; }
}
