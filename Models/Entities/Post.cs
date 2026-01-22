namespace pearlxcore.dev.Models.Entities;

public class Post
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public string Content { get; set; } = null!;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ScheduledPublishAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Author
    public string AuthorId { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;

    // Relations
    public ICollection<PostCategory> PostCategories { get; set; } = new List<PostCategory>();
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public string RenderedContent { get; set; } = null!;

}
