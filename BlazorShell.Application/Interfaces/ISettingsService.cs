using BlazorShell.Application.Models;

namespace BlazorShell.Application.Interfaces;

public interface ISettingsService
{
    Task<IEnumerable<SettingDto>> GetByCategoryAsync(string category);
    Task UpdateSettingAsync(string key, string value);
}
