using Microsoft.AspNetCore.Identity;

namespace pearlxcore.dev.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
