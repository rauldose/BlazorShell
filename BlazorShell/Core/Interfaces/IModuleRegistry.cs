namespace BlazorShell.Core.Interfaces
{
    /// <summary>
    /// Registry for managing loaded modules
    /// </summary>
    public interface IModuleRegistry
    {
        void RegisterModule(IModule module);
        void UnregisterModule(string moduleName);
        IModule GetModule(string moduleName);
        IEnumerable<IModule> GetModules();
        IEnumerable<IModule> GetModulesByCategory(string category);
        bool IsModuleRegistered(string moduleName);
        event EventHandler<ModuleEventArgs> ModuleRegistered;
        event EventHandler<ModuleEventArgs> ModuleUnregistered;
    }
}