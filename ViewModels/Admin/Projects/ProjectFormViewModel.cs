using System.ComponentModel.DataAnnotations;

namespace pearlxcore.dev.ViewModels.Admin.Projects;

public class ProjectFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ProjectType { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Status { get; set; } = "Archived";

    [StringLength(2000)]
    public string? Description { get; set; }

    [Url]
    [StringLength(300)]
    public string? GitHubUrl { get; set; }

    [Url]
    [StringLength(300)]
    public string? ScreenshotUrl { get; set; }

    public IFormFile? ScreenshotFile { get; set; }

    public int SortOrder { get; set; }
}
