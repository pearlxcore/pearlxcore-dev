using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace pearlxcore.dev.Controllers;

public class BlogController : Controller
{
    private readonly IPostService _postService;
    private readonly IAdminProfileService _profileService;

    public BlogController(IPostService postService, IAdminProfileService profileService)
    {
        _postService = postService;
        _profileService = profileService;
    }

    // /blog
    public async Task<IActionResult> Index(int? page)
    {
        const int pageSize = 10;
        var currentPage = page ?? 1;
        
        var paginatedPosts = await _postService.GetPublishedPaginatedAsync(currentPage, pageSize);
        var allPublishedPosts = await _postService.GetPublishedAsync();
        var profile = await _profileService.GetAsync();
        
        ViewData["AdminProfile"] = profile;
        ViewData["AllPublishedPosts"] = allPublishedPosts;
        return View(paginatedPosts);
    }

    // /blog/{slug}
    [HttpGet("/blog/{slug}")]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await _postService.GetPublishedBySlugAsync(slug);

        if (post == null)
            return NotFound();

        var profile = await _profileService.GetAsync();
        ViewData["AdminProfile"] = profile;

        // Get related posts
        var relatedPosts = await _postService.GetRelatedPostsAsync(post.Id, limit: 3);
        ViewData["RelatedPosts"] = relatedPosts;

        return View(post);
    }

    [HttpGet("/blog/category/{slug}")]
    public async Task<IActionResult> Category(string slug, int? page)
    {
        const int pageSize = 10;
        var currentPage = page ?? 1;
        
        var paginatedPosts = await _postService.GetPublishedByCategoryPaginatedAsync(slug, currentPage, pageSize);

        if (paginatedPosts.TotalCount == 0)
            return NotFound();

        var allPublishedPosts = await _postService.GetPublishedAsync();
        var profile = await _profileService.GetAsync();
        ViewData["AdminProfile"] = profile;
        ViewData["AllPublishedPosts"] = allPublishedPosts;
        ViewData["Title"] = $"Category: {slug}";
        ViewData["FilterType"] = "category";
        ViewData["FilterSlug"] = slug;
        return View("Index", paginatedPosts);
    }

    [HttpGet("/blog/tag/{slug}")]
    public async Task<IActionResult> Tag(string slug, int? page)
    {
        const int pageSize = 10;
        var currentPage = page ?? 1;
        
        var paginatedPosts = await _postService.GetPublishedByTagPaginatedAsync(slug, currentPage, pageSize);

        if (paginatedPosts.TotalCount == 0)
            return NotFound();

        var allPublishedPosts = await _postService.GetPublishedAsync();
        var profile = await _profileService.GetAsync();
        ViewData["AdminProfile"] = profile;
        ViewData["AllPublishedPosts"] = allPublishedPosts;
        ViewData["Title"] = $"Tag: {slug}";
        ViewData["FilterType"] = "tag";
        ViewData["FilterSlug"] = slug;
        return View("Index", paginatedPosts);
    }

    [HttpGet("/blog/search")]
    public async Task<IActionResult> Search(string q, int? page)
    {
        const int pageSize = 10;
        var currentPage = page ?? 1;
        
        var paginatedPosts = await _postService.SearchPublishedAsync(q ?? "", currentPage, pageSize);
        var profile = await _profileService.GetAsync();
        
        ViewData["AdminProfile"] = profile;
        ViewData["SearchQuery"] = q;
        ViewData["Title"] = string.IsNullOrWhiteSpace(q) 
            ? "Search Results" 
            : $"Search Results for '{q}'";
        
        return View("Search", paginatedPosts);
    }

}
