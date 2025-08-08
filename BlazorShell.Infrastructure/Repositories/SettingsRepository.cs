using BlazorShell.Domain.Entities;
using BlazorShell.Application.Interfaces.Repositories;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorShell.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Setting>> GetByCategoryAsync(string category) =>
        await _dbContext.Settings
            .Where(s => s.Category == category)
            .OrderBy(s => s.Key)
            .ToListAsync();

    public async Task<Setting?> GetByKeyAsync(string key) =>
        await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);

    public async Task UpdateAsync(Setting setting)
    {
        _dbContext.Settings.Update(setting);
        await Task.CompletedTask;
    }

    public async Task AddAsync(Setting setting)
    {
        await _dbContext.Settings.AddAsync(setting);
    }

    public async Task SaveChangesAsync() => await _dbContext.SaveChangesAsync();
}
