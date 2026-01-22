using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace pearlxcore.dev.Controllers;

public class NewsletterController : Controller
{
    private readonly INewsletterService _newsletterService;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(
        INewsletterService newsletterService,
        ILogger<NewsletterController> logger)
    {
        _newsletterService = newsletterService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(string email, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["NewsletterError"] = "Please enter a valid email address.";
            return Redirect(returnUrl ?? "/");
        }

        if (!IsValidEmail(email))
        {
            TempData["NewsletterError"] = "Please enter a valid email address.";
            return Redirect(returnUrl ?? "/");
        }

        var success = await _newsletterService.SubscribeAsync(email);

        if (success)
        {
            TempData["NewsletterSuccess"] = "Thanks for subscribing! You'll receive our latest posts.";
            _logger.LogInformation("Newsletter subscription: {Email}", email);
            Log.Information("New newsletter subscriber: {Email}, IP: {IP}", email, HttpContext.Connection.RemoteIpAddress);
        }
        else
        {
            TempData["NewsletterError"] = "An error occurred. Please try again later.";
            Log.Warning("Newsletter subscription failed: {Email}", email);
        }

        return Redirect(returnUrl ?? "/");
    }

    [HttpGet]
    public async Task<IActionResult> Unsubscribe(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Log.Warning("Unsubscribe attempt with empty token");
            return NotFound();
        }

        var success = await _newsletterService.UnsubscribeAsync(token);

        if (success)
        {
            Log.Information("Newsletter subscriber unsubscribed successfully");
        }
        else
        {
            Log.Warning("Newsletter unsubscribe failed: Invalid or expired token");
        }

        ViewBag.Success = success;
        return View();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
