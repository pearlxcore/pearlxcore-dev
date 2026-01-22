using pearlxcore.dev.Models.Entities;

namespace pearlxcore.dev.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, int? entityId, string? description, string? userId);
    Task<List<AuditLog>> GetRecentLogsAsync(int count = 100);
    Task<List<AuditLog>> GetLogsByEntityAsync(string entityType, int entityId);
    Task DeleteOldLogsAsync(int daysToKeep = 90);
}
