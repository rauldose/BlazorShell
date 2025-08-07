namespace BlazorShell.Core.Services;

public interface IModuleHotReloadService
{
    Task StartWatchingAsync(string moduleName, string assemblyPath);
    Task StopWatchingAsync(string moduleName);
    Task StopAllWatchersAsync();
    bool IsWatching(string moduleName);
    event EventHandler<ModuleChangedEventArgs>? ModuleChanged;
}
