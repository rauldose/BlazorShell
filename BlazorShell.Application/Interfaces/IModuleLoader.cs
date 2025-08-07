namespace BlazorShell.Application.Interfaces;

public interface IModuleLoader
{
    Task InitializeModulesAsync();
    Task<IModule> LoadModuleAsync(string assemblyPath);
    Task<bool> UnloadModuleAsync(string moduleName);
    Task<IEnumerable<IModule>> GetLoadedModulesAsync();
    Task<IModule> GetModuleAsync(string moduleName);
    Task ReloadModuleAsync(string moduleName);
}

