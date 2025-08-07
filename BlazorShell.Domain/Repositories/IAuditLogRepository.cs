using BlazorShell.Domain.Entities;

namespace BlazorShell.Domain.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<List<AuditLog>> GetLogsAsync(int skip, int take);
    Task<List<AuditLog>> GetLogsByUserAsync(string userId, int skip, int take);
    Task<List<AuditLog>> GetLogsByEntityAsync(string entityName, string entityId, int skip, int take);
    Task SaveChangesAsync();
}
