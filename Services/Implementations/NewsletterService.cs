using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace pearlxcore.dev.Services.Implementations;

public class NewsletterService : INewsletterService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(ApplicationDbContext db, ILogger<NewsletterService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> SubscribeAsync(string email)
    {
        try
        {
            email = email.Trim().ToLower();

            var existing = await _db.NewsletterSubscribers
                .FirstOrDefaultAsync(s => s.Email == email);

            if (existing != null)
            {
                if (existing.IsActive)
                {
                    _logger.LogInformation("Email {Email} is already subscribed", email);
                    return true; // Already subscribed
                }
                else
                {
                    // Reactivate subscription
                    existing.IsActive = true;
                    existing.SubscribedAt = DateTime.UtcNow;
                    existing.UnsubscribedAt = null;
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Reactivated subscription for {Email}", email);
                    return true;
                }
            }

            var subscriber = new NewsletterSubscriber
            {
                Email = email,
                IsActive = true,
                UnsubscribeToken = GenerateUnsubscribeToken()
            };

            _db.NewsletterSubscribers.Add(subscriber);
            await _db.SaveChangesAsync();

            _logger.LogInformation("New subscription created for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing email {Email}", email);
            return false;
        }
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        try
        {
            var subscriber = await _db.NewsletterSubscribers
                .FirstOrDefaultAsync(s => s.UnsubscribeToken == token && s.IsActive);

            if (subscriber == null)
            {
                _logger.LogWarning("Unsubscribe token not found or already unsubscribed: {Token}", token);
                return false;
            }

            subscriber.IsActive = false;
            subscriber.UnsubscribedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Unsubscribed {Email}", subscriber.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing with token {Token}", token);
            return false;
        }
    }

    public async Task<NewsletterSubscriber?> GetByEmailAsync(string email)
    {
        return await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email.ToLower());
    }

    public async Task<IEnumerable<NewsletterSubscriber>> GetAllActiveAsync()
    {
        return await _db.NewsletterSubscribers
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.SubscribedAt)
            .ToListAsync();
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _db.NewsletterSubscribers
            .CountAsync(s => s.IsActive);
    }

    public async Task<bool> IsSubscribedAsync(string email)
    {
        return await _db.NewsletterSubscribers
            .AnyAsync(s => s.Email == email.ToLower() && s.IsActive);
    }

    private static string GenerateUnsubscribeToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 32);
    }
}
