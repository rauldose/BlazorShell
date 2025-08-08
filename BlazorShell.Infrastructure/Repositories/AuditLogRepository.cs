using BlazorShell.Domain.Entities;
using BlazorShell.Application.Interfaces.Repositories;
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

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count) =>
        await _dbContext.AuditLogs
            .OrderByDescending(a => a.CreatedDate)
            .Take(count)
            .ToListAsync();
}
