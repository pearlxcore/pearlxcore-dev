using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.ViewModels.Admin.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace pearlxcore.dev.Areas.Admin.Controllers;

public class DashboardController : AdminController
{
    private readonly IPostService _postService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly IAdminProfileService _profileService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public DashboardController(
        IPostService postService,
        ICategoryService categoryService,
        ITagService tagService,
        IAdminProfileService profileService,
        IWebHostEnvironment webHostEnvironment)
    {
        _postService = postService;
        _categoryService = categoryService;
        _tagService = tagService;
        _profileService = profileService;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new DashboardViewModel
        {
            TotalPosts = await _postService.GetTotalCountAsync(),
            PublishedPosts = await _postService.GetPublishedCountAsync(),
            DraftPosts = await _postService.GetDraftCountAsync(),
            ScheduledPosts = await _postService.GetScheduledCountAsync(),
            TotalCategories = (await _categoryService.GetAllAsync()).Count(),
            TotalTags = (await _tagService.GetAllAsync()).Count(),
            RecentPosts = await _postService.GetRecentPostsAsync(5),
            RecentDrafts = await _postService.GetRecentDraftsAsync(5),
            ScheduledPostsList = await _postService.GetScheduledPostsAsync(5),
            AdminProfile = await _profileService.GetAsync()
        };

        // Get last published post
        var lastPublished = await _postService.GetLastPublishedPostAsync();
        if (lastPublished != null)
        {
            viewModel.LastPublishedDate = lastPublished.PublishedAt;
            viewModel.LastPublishedTitle = lastPublished.Title;
        }

        // Calculate image storage
        var imagesPaths = new[]
        {
            Path.GetFullPath(Path.Combine(_webHostEnvironment.ContentRootPath, "..", "..", "shared", "uploads", "posts")),
            Path.Combine(_webHostEnvironment.WebRootPath, "images", "posts")
        };

        var imageFiles = imagesPaths
            .Where(Directory.Exists)
            .SelectMany(path => Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            .ToList();

        if (imageFiles.Any())
        {
            viewModel.TotalImagesCount = imageFiles.Count;
            viewModel.TotalImagesSize = imageFiles.Sum(f => new FileInfo(f).Length);
        }

        return View(viewModel);
    }
}
