// BlazorShell.Modules.Admin/Services/AuditService.cs
using Microsoft.EntityFrameworkCore;
using BlazorShell.Domain.Entities;
using BlazorShell.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BlazorShell.Modules.Admin.Services.Interfaces;

namespace BlazorShell.Modules.Admin.Services.Implementations
{
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IAuditLogRepository auditRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _auditRepository = auditRepository;
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

                await _auditRepository.AddAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit entry");
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50)
        {
            return await _auditRepository.GetLogsAsync((page - 1) * pageSize, pageSize);
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int page = 1, int pageSize = 50)
        {
            return await _auditRepository.GetLogsByUserAsync(userId, (page - 1) * pageSize, pageSize);
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityName, string entityId, int page = 1, int pageSize = 50)
        {
            return await _auditRepository.GetLogsByEntityAsync(entityName, entityId, (page - 1) * pageSize, pageSize);
        }
    }
}
