namespace pearlxcore.dev.ViewModels.Admin.Posts;

public class PostFormViewModel
{
    public int? Id { get; set; }

    public string Title { get; set; } = null!;
    public string? Slug { get; set; }
    public string Content { get; set; } = null!;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool RemoveImage { get; set; }

    public bool IsPublished { get; set; }
    public string? ScheduledPublishDate { get; set; }
    public string? ScheduledPublishTime { get; set; }
    public List<int> CategoryIds { get; set; } = new();
    public List<int> TagIds { get; set; } = new();

}
