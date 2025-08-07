namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Module loader responsible for discovering and loading modules
    /// </summary>
    public interface IModuleLoader
    {
        Task InitializeModulesAsync();
        Task<IModule> LoadModuleAsync(string assemblyPath);
        Task<bool> UnloadModuleAsync(string moduleName);
        Task<IEnumerable<IModule>> GetLoadedModulesAsync();
        Task<IModule> GetModuleAsync(string moduleName);
        Task ReloadModuleAsync(string moduleName);
    }
}