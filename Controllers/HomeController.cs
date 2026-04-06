using System.Diagnostics;
using pearlxcore.dev.Models;
using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace pearlxcore.dev.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPostService _postService;
        private readonly IAdminProfileService _profileService;

        public HomeController(IPostService postService, IAdminProfileService profileService)
        {
            _postService = postService;
            _profileService = profileService;
        }

        public async Task<IActionResult> Index(int? page)
        {
            const int pageSize = 10;
            var currentPage = page ?? 1;

            var paginatedPosts = await _postService.GetPublishedPaginatedAsync(currentPage, pageSize);
            var allPublishedPosts = await _postService.GetPublishedAsync();
            var profile = await _profileService.GetAsync();

            ViewData["AdminProfile"] = profile;
            ViewData["AllPublishedPosts"] = allPublishedPosts;

            return View("~/Views/Blog/Index.cshtml", paginatedPosts);
        }

        public async Task<IActionResult> About()
        {
            var profile = await _profileService.GetAsync();
            return View(profile);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public new IActionResult StatusCode(int code)
        {
            if (code == 404)
                return View("NotFound");

            return View("Error");
        }

    }
}
