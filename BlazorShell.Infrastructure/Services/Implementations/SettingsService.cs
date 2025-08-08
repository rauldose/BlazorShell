using System;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Models;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorShell.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _dbContext;

    public SettingsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SettingDto>> GetByCategoryAsync(string category)
    {
        return await _dbContext.Settings
            .Where(s => s.Category == category)
            .Select(s => new SettingDto
            {
                Key = s.Key ?? string.Empty,
                Value = s.Value,
                DataType = s.DataType ?? string.Empty,
                Description = s.Description,
                IsEncrypted = s.IsEncrypted
            })
            .ToListAsync();
    }

    public async Task UpdateSettingAsync(string key, string value)
    {
        var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            setting.Value = value;
            setting.ModifiedDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
}

