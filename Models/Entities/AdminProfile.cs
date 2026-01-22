using System;

namespace pearlxcore.dev.Models.Entities;

public class AdminProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CvUrl { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
