using pearlxcore.dev.Web.Data;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace pearlxcore.dev.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, string entityType, int? entityId, string? description, string? userId)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _context.AuditLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetLogsByEntityAsync(string entityType, int entityId)
    {
        return await _context.AuditLogs
            .Include(l => l.User)
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteOldLogsAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldLogs = await _context.AuditLogs
            .Where(l => l.CreatedAt < cutoffDate)
            .ToListAsync();

        _context.AuditLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
    }
}
