// BlazorShell.Modules.Admin/Services/AuditService.cs
using Microsoft.EntityFrameworkCore;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Admin.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(string action, string entityName, string? entityId, string? userId, object? oldValues = null, object? newValues = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var auditLog = new AuditLog
                {
                    UserId = userId ?? httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    UserName = httpContext?.User?.Identity?.Name,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    OldValues = oldValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(oldValues) : null,
                    NewValues = newValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(newValues) : null,
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit entry");
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50)
        {
            return await _dbContext.AuditLogs
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int page = 1, int pageSize = 50)
        {
            return await _dbContext.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityName, string entityId, int page = 1, int pageSize = 50)
        {
            return await _dbContext.AuditLogs
                .Where(a => a.EntityName == entityName && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
