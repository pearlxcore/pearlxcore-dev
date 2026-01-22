using pearlxcore.dev.Models;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Models.Entities;

namespace pearlxcore.dev.Services.Interfaces;

public interface IPostService
{
    Task<IEnumerable<Post>> GetAllAsync();
    Task<PaginatedList<Post>> GetAllPaginatedAsync(int pageIndex = 1, int pageSize = 10);
    Task<Post?> GetByIdAsync(int id);
    Task CreateAsync(Post post, IEnumerable<int> categoryIds, IEnumerable<int> tagIds);
    Task UpdateAsync(Post post, IEnumerable<int> categoryIds, IEnumerable<int> tagIds);
    Task DeleteAsync(int id);

    Task<bool> SlugExistsAsync(string slug, int? ignorePostId = null);
    Task<IEnumerable<Post>> GetPublishedAsync();
    Task<PaginatedList<Post>> GetPublishedPaginatedAsync(int pageIndex = 1, int pageSize = 10);
    Task<Post?> GetPublishedBySlugAsync(string slug);
    Task<IEnumerable<Post>> GetPublishedByCategoryAsync(string categorySlug);
    Task<PaginatedList<Post>> GetPublishedByCategoryPaginatedAsync(string categorySlug, int pageIndex = 1, int pageSize = 10);
    Task<IEnumerable<Post>> GetPublishedByTagAsync(string tagSlug);
    Task<PaginatedList<Post>> GetPublishedByTagPaginatedAsync(string tagSlug, int pageIndex = 1, int pageSize = 10);

    Task<PaginatedList<Post>> SearchPublishedAsync(string query, int pageIndex = 1, int pageSize = 10);

    Task<string?> SavePostImageAsync(IFormFile? imageFile);
    
    // Dashboard Statistics
    Task<int> GetTotalCountAsync();
    Task<int> GetPublishedCountAsync();
    Task<int> GetDraftCountAsync();
    Task<int> GetScheduledCountAsync();
    Task<List<Post>> GetRecentPostsAsync(int count = 5);
    Task<List<Post>> GetRecentDraftsAsync(int count = 5);
    Task<List<Post>> GetScheduledPostsAsync(int count = 5);
    Task<Post?> GetLastPublishedPostAsync();
    
    // Related Posts
    Task<IEnumerable<Post>> GetRelatedPostsAsync(int postId, int limit = 3);

}
