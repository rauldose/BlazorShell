using BlazorShell.Domain.Entities;
using BlazorShell.Application.Interfaces.Repositories;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorShell.Infrastructure.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ModuleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Module>> GetAllAsync() => await _dbContext.Modules.Include(m => m.NavigationItems).ToListAsync();

    public async Task<Module?> GetByNameAsync(string name) =>
        await _dbContext.Modules.Include(m => m.NavigationItems).FirstOrDefaultAsync(m => m.Name == name);

    public async Task AddAsync(Module module)
    {
        _dbContext.Modules.Add(module);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveAsync(Module module)
    {
        _dbContext.Modules.Remove(module);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync() => await _dbContext.SaveChangesAsync();
}
