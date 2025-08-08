using BlazorShell.Domain.Entities;

namespace BlazorShell.Application.Interfaces.Repositories;

public interface ISettingsRepository
{
    Task<List<Setting>> GetByCategoryAsync(string category);
    Task<Setting?> GetByKeyAsync(string key);
    Task UpdateAsync(Setting setting);
    Task AddAsync(Setting setting);
    Task SaveChangesAsync();
}
