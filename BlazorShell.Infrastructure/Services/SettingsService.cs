using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Interfaces.Repositories;
using BlazorShell.Application.Models;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Application.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SettingsService> _logger;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SETTINGS_CACHE_KEY = "app_settings_cache_";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public SettingsService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<SettingsService> logger,
            IAuditLogRepository auditLogRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<IEnumerable<SettingDto>> GetByCategoryAsync(string category)
        {
            try
            {
                var cacheKey = $"{SETTINGS_CACHE_KEY}{category}";

                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;

                    var settings = await _context.Settings
                        .Where(s => s.Category == category)
                        .OrderBy(s => s.DisplayOrder)
                        .ThenBy(s => s.Key)
                        .Select(s => new SettingDto
                        {
                            Key = s.Key,
                            Value = s.Value,
                            Category = s.Category,
                            Description = s.Description,
                            DataType = s.DataType ?? "string",
                            IsVisible = s.IsVisible,
                            IsReadOnly = s.IsReadOnly,
                            DisplayOrder = s.DisplayOrder,
                            DefaultValue = s.DefaultValue
                        })
                        .ToListAsync();

                    _logger.LogDebug("Loaded {Count} settings for category {Category}", settings.Count, category);
                    return settings;
                }) ?? Enumerable.Empty<SettingDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings for category {Category}", category);
                return Enumerable.Empty<SettingDto>();
            }
        }

        public async Task UpdateSettingAsync(string key, string value)
        {
            try
            {
                var setting = await _context.Settings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (setting == null)
                {
                    _logger.LogWarning("Attempted to update non-existent setting: {Key}", key);
                    throw new KeyNotFoundException($"Setting with key '{key}' not found.");
                }

                var oldValue = setting.Value;
                var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

                // Update the setting
                setting.Value = value;
                setting.ModifiedDate = DateTime.UtcNow;
                setting.ModifiedBy = userId;

                await _context.SaveChangesAsync();

                // Clear cache for the category
                if (!string.IsNullOrEmpty(setting.Category))
                {
                    _cache.Remove($"{SETTINGS_CACHE_KEY}{setting.Category}");
                }

                // Create audit log
                await CreateAuditLogAsync(setting, oldValue, value, userId);

                _logger.LogInformation("Setting {Key} updated from '{OldValue}' to '{NewValue}' by {UserId}",
                    key, oldValue, value, userId);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {Key} to {Value}", key, value);
                throw new InvalidOperationException($"Failed to update setting '{key}'", ex);
            }
        }

        private async Task CreateAuditLogAsync(Setting setting, string oldValue, string newValue, string userId)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var auditLog = new AuditLog
                {
                    UserId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    UserName = userId,
                    Action = "UpdateSetting",
                    EntityName = "Setting",
                    EntityId = setting.Key.ToString(),
                    OldValues = JsonSerializer.Serialize(new { Key = setting.Key, Value = oldValue }),
                    NewValues = JsonSerializer.Serialize(new { Key = setting.Key, Value = newValue }),
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                await _auditLogRepository.AddAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log for setting update");
                // Don't throw - audit logging failure shouldn't stop the operation
            }
        }
    }
}