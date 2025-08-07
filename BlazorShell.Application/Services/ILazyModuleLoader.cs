using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Models;

namespace BlazorShell.Application.Services;

public interface ILazyModuleLoader
{
    Task<IModule?> LoadModuleOnDemandAsync(string moduleName);
    Task PreloadModulesAsync(params string[] moduleNames);
    Task<bool> IsModuleLoadedAsync(string moduleName);
    Task<ModuleLoadStatus> GetModuleStatusAsync(string moduleName);
    void SetModuleLoadingStrategy(ModuleLoadingStrategy strategy);
    Task UnloadInactiveModulesAsync(TimeSpan inactiveThreshold);
    IEnumerable<ModuleLoadStatus> GetAllModuleStatuses();
}
