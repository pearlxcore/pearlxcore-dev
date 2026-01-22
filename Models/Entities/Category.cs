namespace pearlxcore.dev.Models.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public ICollection<PostCategory> PostCategories { get; set; } = new List<PostCategory>();
}
