using pearlxcore.dev.Models.Entities;

namespace pearlxcore.dev.Services.Interfaces;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(int id);
    Task CreateAsync(Tag tag);
    Task DeleteAsync(int id);
}
