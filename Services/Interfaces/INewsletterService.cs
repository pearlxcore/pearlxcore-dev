using pearlxcore.dev.Models.Entities;

namespace pearlxcore.dev.Services.Interfaces;

public interface INewsletterService
{
    Task<bool> SubscribeAsync(string email);
    Task<bool> UnsubscribeAsync(string token);
    Task<NewsletterSubscriber?> GetByEmailAsync(string email);
    Task<IEnumerable<NewsletterSubscriber>> GetAllActiveAsync();
    Task<int> GetActiveCountAsync();
    Task<bool> IsSubscribedAsync(string email);
}
