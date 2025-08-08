using BlazorShell.Domain.Entities;

namespace BlazorShell.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count);
}
