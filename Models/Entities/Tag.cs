namespace pearlxcore.dev.Models.Entities;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}
