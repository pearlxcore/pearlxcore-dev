using System.ComponentModel.DataAnnotations;

namespace pearlxcore.dev.ViewModels.Admin.Profile;

public class AdminProfileFormViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    [Display(Name = "Avatar Image")]
    public IFormFile? AvatarFile { get; set; }

    public string? CvUrl { get; set; }

    [Display(Name = "Resume/CV (PDF)")]
    public IFormFile? CvFile { get; set; }
}
