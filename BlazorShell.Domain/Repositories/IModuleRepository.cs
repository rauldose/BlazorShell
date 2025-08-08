using BlazorShell.Domain.Entities;

namespace BlazorShell.Domain.Repositories;

public interface IModuleRepository
{
    Task<List<Module>> GetAllAsync();
    Task<Module?> GetByNameAsync(string name);
    Task AddAsync(Module module);
    Task RemoveAsync(Module module);
    Task SaveChangesAsync();
}
