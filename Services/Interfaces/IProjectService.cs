using pearlxcore.dev.Models.Entities;

namespace pearlxcore.dev.Services.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(int id);
    Task CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(int id);
}
