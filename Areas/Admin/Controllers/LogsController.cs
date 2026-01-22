using pearlxcore.dev.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace pearlxcore.dev.Areas.Admin.Controllers;

public class LogsController : AdminController
{
    private readonly IAuditLogService _auditLogService;

    public LogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _auditLogService.GetRecentLogsAsync(500);
        return View(logs);
    }
}
