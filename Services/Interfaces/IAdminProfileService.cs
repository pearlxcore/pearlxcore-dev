using pearlxcore.dev.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace pearlxcore.dev.Services.Interfaces;

public interface IAdminProfileService
{
    Task<AdminProfile> GetAsync();
    Task SaveAsync(AdminProfile profile);
    Task<string?> SaveAvatarAsync(IFormFile? avatarFile);
    Task<string?> SaveCvAsync(IFormFile? cvFile);
}
