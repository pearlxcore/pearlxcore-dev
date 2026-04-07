using pearlxcore.dev.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace pearlxcore.dev.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigurePost(builder);
        ConfigureCategory(builder);
        ConfigureTag(builder);
        ConfigurePostCategory(builder);
        ConfigurePostTag(builder);
            ConfigureAdminProfile(builder);
        ConfigureProject(builder);
    }

    private static void ConfigurePost(ModelBuilder builder)
    {
        builder.Entity<Post>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.RenderedContent)
                .IsRequired();


            entity.Property(p => p.Title)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(p => p.Slug)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(p => p.Content)
                  .IsRequired();

            entity.Property(p => p.Summary)
                  .HasMaxLength(500);

            entity.HasIndex(p => p.Slug)
                  .IsUnique();

            entity.HasIndex(p => p.PublishedAt);

            entity.HasOne(p => p.Author)
                  .WithMany(u => u.Posts)
                  .HasForeignKey(p => p.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCategory(ModelBuilder builder)
    {
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(c => c.Slug)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.HasIndex(c => c.Slug)
                  .IsUnique();
        });
    }

    private static void ConfigureTag(ModelBuilder builder)
    {
        builder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(t => t.Slug)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.HasIndex(t => t.Slug)
                  .IsUnique();
        });
    }

    private static void ConfigurePostCategory(ModelBuilder builder)
    {
        builder.Entity<PostCategory>(entity =>
        {
            entity.HasKey(pc => new { pc.PostId, pc.CategoryId });

            entity.HasOne(pc => pc.Post)
                  .WithMany(p => p.PostCategories)
                  .HasForeignKey(pc => pc.PostId);

            entity.HasOne(pc => pc.Category)
                  .WithMany(c => c.PostCategories)
                  .HasForeignKey(pc => pc.CategoryId);
        });
    }

    private static void ConfigurePostTag(ModelBuilder builder)
    {
        builder.Entity<PostTag>(entity =>
        {
            entity.HasKey(pt => new { pt.PostId, pt.TagId });

            entity.HasOne(pt => pt.Post)
                  .WithMany(p => p.PostTags)
                  .HasForeignKey(pt => pt.PostId);

            entity.HasOne(pt => pt.Tag)
                  .WithMany(t => t.PostTags)
                  .HasForeignKey(pt => pt.TagId);
        });
    }

    private static void ConfigureAdminProfile(ModelBuilder builder)
    {
        builder.Entity<AdminProfile>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(p => p.Title)
                  .HasMaxLength(150);

            entity.Property(p => p.AvatarUrl)
                  .HasMaxLength(300);

            entity.Property(p => p.Bio)
                  .HasColumnType("nvarchar(max)");

            entity.Property(p => p.UpdatedAt)
                  .IsRequired();
        });
    }

    private static void ConfigureProject(ModelBuilder builder)
    {
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Title)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(p => p.ProjectType)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(p => p.Platform)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(p => p.Status)
                  .HasMaxLength(40)
                  .IsRequired();

            entity.Property(p => p.Summary)
                  .HasMaxLength(500);

            entity.Property(p => p.Description)
                  .HasColumnType("nvarchar(max)");

            entity.Property(p => p.GitHubUrl)
                  .HasMaxLength(300);

            entity.Property(p => p.DownloadUrl)
                  .HasMaxLength(300);

            entity.Property(p => p.ScreenshotUrl)
                  .HasMaxLength(300);

            entity.Property(p => p.CreatedAt)
                  .IsRequired();

            entity.Property(p => p.UpdatedAt)
                  .IsRequired();

            entity.HasIndex(p => p.SortOrder);
        });
    }

}
