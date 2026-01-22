using pearlxcore.dev.Areas.Admin.Controllers;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.ViewModels.Admin.Profile;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace pearlxcore.dev.Areas.Admin.Controllers;

public class ProfileController : AdminController
{
    private readonly IAdminProfileService _profileService;

    public ProfileController(IAdminProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> About()
    {
        var profile = await _profileService.GetAsync();
        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var profile = await _profileService.GetAsync();
        var vm = new AdminProfileFormViewModel
        {
            Name = profile.Name,
            Title = profile.Title,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            CvUrl = profile.CvUrl
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminProfileFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var existingProfile = await _profileService.GetAsync();
        var avatarUrl = existingProfile.AvatarUrl;
        var cvUrl = existingProfile.CvUrl;

        // Handle avatar upload if file provided
        if (vm.AvatarFile != null)
        {
            var newAvatarUrl = await _profileService.SaveAvatarAsync(vm.AvatarFile);
            if (newAvatarUrl != null)
            {
                avatarUrl = newAvatarUrl;
            }
            else
            {
                ModelState.AddModelError("AvatarFile", "Invalid image file. Supported formats: JPG, PNG, GIF, WebP. Max size: 5MB");
                return View(vm);
            }
        }

        // Handle CV upload if file provided
        if (vm.CvFile != null)
        {
            var newCvUrl = await _profileService.SaveCvAsync(vm.CvFile);
            if (newCvUrl != null)
            {
                cvUrl = newCvUrl;
            }
            else
            {
                ModelState.AddModelError("CvFile", "Invalid file. Supported formats: PDF, DOC, DOCX. Max size: 10MB");
                return View(vm);
            }
        }

        var profile = new AdminProfile
        {
            Name = vm.Name,
            Title = vm.Title,
            Bio = vm.Bio,
            AvatarUrl = avatarUrl,
            CvUrl = cvUrl
        };

        await _profileService.SaveAsync(profile);
        Log.Information("Admin profile updated by {User}: Name={Name}, HasAvatar={HasAvatar}, HasCV={HasCV}", 
            User.Identity?.Name, vm.Name, !string.IsNullOrEmpty(avatarUrl), !string.IsNullOrEmpty(cvUrl));
        TempData["ProfileUpdated"] = true;
        return RedirectToAction(nameof(About));
    }
}
