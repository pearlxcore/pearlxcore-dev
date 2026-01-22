using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace pearlxcore.dev.Areas.Admin.Controllers;

public class NewsletterController : AdminController
{
    private readonly INewsletterService _newsletterService;

    public NewsletterController(INewsletterService newsletterService)
    {
        _newsletterService = newsletterService;
    }

    public async Task<IActionResult> Index()
    {
        var subscribers = await _newsletterService.GetAllActiveAsync();
        return View(subscribers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string email)
    {
        var subscriber = await _newsletterService.GetByEmailAsync(email);
        if (subscriber != null && !string.IsNullOrEmpty(subscriber.UnsubscribeToken))
        {
            await _newsletterService.UnsubscribeAsync(subscriber.UnsubscribeToken);
        }

        return RedirectToAction(nameof(Index));
    }
}
