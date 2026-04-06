using pearlxcore.dev.Models.Entities;

namespace pearlxcore.dev.ViewModels.Admin.Dashboard;

public class DashboardViewModel
{
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int ScheduledPosts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalTags { get; set; }
    
    public List<Post> RecentPosts { get; set; } = new();
    public List<Post> RecentDrafts { get; set; } = new();
    public List<Post> ScheduledPostsList { get; set; } = new();
    
    public AdminProfile? AdminProfile { get; set; }
    
    public DateTime? LastPublishedDate { get; set; }
    public string? LastPublishedTitle { get; set; }
    
    public long TotalImagesSize { get; set; }
    public int TotalImagesCount { get; set; }
    
    public string FormattedImageSize => FormatBytes(TotalImagesSize);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
