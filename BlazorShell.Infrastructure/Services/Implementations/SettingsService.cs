using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Interfaces.Repositories;
using BlazorShell.Application.Models;
using BlazorShell.Domain.Entities;

namespace BlazorShell.Infrastructure.Services.Implementations;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;

    public SettingsService(ISettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SettingDto>> GetByCategoryAsync(string category)
    {
        var settings = await _repository.GetByCategoryAsync(category);
        return settings.Select(s => new SettingDto
        {
            Category = s.Category,
            Key = s.Key,
            Value = s.Value,
            DataType = s.DataType,
            Description = s.Description,
            IsEncrypted = s.IsEncrypted,
            IsPublic = s.IsPublic,
            ModifiedDate = s.ModifiedDate,
            ModifiedBy = s.ModifiedBy
        });
    }

    public async Task UpdateSettingAsync(string key, string value)
    {
        var setting = await _repository.GetByKeyAsync(key);
        if (setting == null)
        {
            setting = new Setting { Key = key, Value = value, ModifiedDate = DateTime.UtcNow, ModifiedBy = "System" };
            await _repository.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            setting.ModifiedDate = DateTime.UtcNow;
            setting.ModifiedBy = "System";
        }
        await _repository.SaveChangesAsync();
    }
}
