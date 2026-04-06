using pearlxcore.dev.Services.Interfaces;
using Serilog;

namespace pearlxcore.dev.Services.Implementations;

public class MediaService : IMediaService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public MediaService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<List<MediaFileInfo>> GetAllImagesAsync()
    {
        var imagesPath = GetImageDirectories();
        var mediaFiles = new List<MediaFileInfo>();

        if (!imagesPath.Any())
            return mediaFiles;

        var files = imagesPath
            .Where(Directory.Exists)
            .SelectMany(path => Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
            .Where(f => IsImageFile(f))
            .GroupBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(f => new FileInfo(f).CreationTime).First())
            .OrderByDescending(f => new FileInfo(f).CreationTime)
            .ToList();

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            mediaFiles.Add(new MediaFileInfo
            {
                FileName = fileInfo.Name,
                FilePath = $"/images/posts/{Path.GetFileName(file)}",
                FileSize = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime
            });
        }

        return await Task.FromResult(mediaFiles);
    }

    public async Task<bool> DeleteImageAsync(string fileName)
    {
        try
        {
            foreach (var imagesPath in GetImageDirectories())
            {
                var filePath = Path.Combine(imagesPath, fileName);

                // Security: Ensure the file is within the images/posts directory
                var fullPath = Path.GetFullPath(filePath);
                var fullImagesPath = Path.GetFullPath(imagesPath);

                if (!fullPath.StartsWith(fullImagesPath))
                {
                    continue;
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Log.Information("Image deleted: {FileName}", fileName);
                    return await Task.FromResult(true);
                }
            }

            Log.Warning("Image file not found for deletion: {FileName}", fileName);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting image: {FileName}", fileName);
            return false;
        }
    }

    private static bool IsImageFile(string filePath)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(filePath).ToLower();
        return allowedExtensions.Contains(extension);
    }

    private List<string> GetImageDirectories()
    {
        return
        [
            Path.GetFullPath(Path.Combine(_webHostEnvironment.ContentRootPath, "..", "..", "shared", "uploads", "posts")),
            Path.Combine(_webHostEnvironment.WebRootPath, "images", "posts")
        ];
    }
}
