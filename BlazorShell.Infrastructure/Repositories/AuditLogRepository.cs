using BlazorShell.Domain.Entities;
using BlazorShell.Domain.Repositories;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorShell.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog log)
    {
        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync(int skip, int take) =>
        await _dbContext.AuditLogs
            .OrderByDescending(a => a.CreatedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

    public async Task<List<AuditLog>> GetLogsByUserAsync(string userId, int skip, int take) =>
        await _dbContext.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

    public async Task<List<AuditLog>> GetLogsByEntityAsync(string entityName, string entityId, int skip, int take) =>
        await _dbContext.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

    public async Task SaveChangesAsync() => await _dbContext.SaveChangesAsync();
}
