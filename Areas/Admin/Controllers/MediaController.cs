using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace pearlxcore.dev.Areas.Admin.Controllers;

public class MediaController : AdminController
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public async Task<IActionResult> Index()
    {
        var images = await _mediaService.GetAllImagesAsync();
        return View(images);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Log.Warning("Media delete attempt with empty fileName");
            return BadRequest();
        }

        var success = await _mediaService.DeleteImageAsync(fileName);
        if (success)
        {
            Log.Information("Admin deleted media file: {FileName}, User: {User}", fileName, User.Identity?.Name);
            TempData["SuccessMessage"] = $"Image '{fileName}' deleted successfully.";
        }
        else
        {
            Log.Warning("Admin failed to delete media file: {FileName}, User: {User}", fileName, User.Identity?.Name);
            TempData["ErrorMessage"] = "Failed to delete image.";
        }

        return RedirectToAction(nameof(Index));
    }
}
