using pearlxcore.dev.Infrastructure;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.ViewModels.Admin.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace pearlxcore.dev.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = IdentityRoles.Admin)]
public class SettingsController : AdminController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<SettingsController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("Settings page accessed by non-existent user");
            return NotFound();
        }

        ViewData["Title"] = "Settings";
        return View();
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Change Password";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("Change password attempted by non-existent user");
            return NotFound();
        }

        // Verify current password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Incorrect current password provided by user: {Email}", user.Email);
            ModelState.AddModelError(string.Empty, "Current password is incorrect.");
            return View(model);
        }

        // Change password
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to change password for user: {Email}, Errors: {Errors}",
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Sign in again with new password
        await _signInManager.RefreshSignInAsync(user);
        
        _logger.LogInformation("Password changed successfully for user: {Email}, IP: {IP}",
            user.Email, HttpContext.Connection.RemoteIpAddress);
        
        TempData["SuccessMessage"] = "Your password has been changed successfully.";
        return RedirectToAction("Index");
    }
}
