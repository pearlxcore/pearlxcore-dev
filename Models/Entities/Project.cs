using System.ComponentModel.DataAnnotations;

namespace pearlxcore.dev.Models.Entities;

public class Project
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ProjectType { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = "Archived";

    [MaxLength(500)]
    public string? Summary { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(300)]
    public string? GitHubUrl { get; set; }

    [MaxLength(300)]
    public string? DownloadUrl { get; set; }

    [MaxLength(300)]
    public string? ScreenshotUrl { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
