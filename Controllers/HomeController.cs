using System.Diagnostics;
using pearlxcore.dev.Models;
using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace pearlxcore.dev.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAdminProfileService _profileService;

        public HomeController(IAdminProfileService profileService)
        {
            _profileService = profileService;
        }

        public IActionResult Index()
        {
            // Redirect to blog as the main page
            return RedirectToAction("Index", "Blog");
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

        public IActionResult StatusCode(int code)
        {
            if (code == 404)
                return View("NotFound");

            return View("Error");
        }

    }
}
