using pearlxcore.dev.Web.Data;
using pearlxcore.dev.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace pearlxcore.dev.Services.BackgroundServices;

public class PostPublishingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PostPublishingService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public PostPublishingService(
        IServiceProvider serviceProvider,
        ILogger<PostPublishingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Post Publishing Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndPublishScheduledPosts(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking scheduled posts");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Post Publishing Service stopped");
    }

    private async Task CheckAndPublishScheduledPosts(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var scheduledPosts = await dbContext.Posts
            .Where(p => !p.IsPublished && p.ScheduledPublishAt != null && p.ScheduledPublishAt <= now)
            .ToListAsync(cancellationToken);

        if (scheduledPosts.Any())
        {
            _logger.LogInformation("Found {Count} posts ready to publish", scheduledPosts.Count);

            foreach (var post in scheduledPosts)
            {
                post.IsPublished = true;
                post.PublishedAt = DateTime.UtcNow;
                post.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Auto-published post: {Title} (ID: {Id})", post.Title, post.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
