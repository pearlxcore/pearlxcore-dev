using pearlxcore.dev.Models.Entities;

namespace pearlxcore.dev.Services.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task CreateAsync(Category category);
    Task DeleteAsync(int id);
}
