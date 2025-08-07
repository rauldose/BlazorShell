// BlazorShell.Modules.Admin/Services/Interfaces/IAuditService.cs
using BlazorShell.Domain.Entities;

namespace BlazorShell.Modules.Admin.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId, string? userId, object? oldValues = null, object? newValues = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityName, string entityId, int page = 1, int pageSize = 50);
}

