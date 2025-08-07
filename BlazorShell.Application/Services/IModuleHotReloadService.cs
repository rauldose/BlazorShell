using System;
using System.Threading.Tasks;
using BlazorShell.Application.Models;

namespace BlazorShell.Application.Services;

public interface IModuleHotReloadService
{
    Task StartWatchingAsync(string moduleName, string assemblyPath);
    Task StopWatchingAsync(string moduleName);
    Task StopAllWatchersAsync();
    bool IsWatching(string moduleName);
    event EventHandler<ModuleChangedEventArgs>? ModuleChanged;
}
