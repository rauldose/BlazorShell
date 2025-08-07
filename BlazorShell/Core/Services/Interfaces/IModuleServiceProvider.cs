using System;
using System.Collections.Generic;
using BlazorShell.Core.Interfaces;

namespace BlazorShell.Core.Services;

public interface IModuleServiceProvider
{
    void RegisterModuleServices(string moduleName, IServiceModule module);
    void UnregisterModuleServices(string moduleName);
    T? GetService<T>() where T : class;
    object? GetService(Type serviceType);
    IServiceProvider GetModuleServiceProvider(string moduleName);
    bool IsModuleRegistered(string moduleName);
    void RefreshModuleServices(string moduleName, IServiceModule module);
    IEnumerable<string> GetRegisteredModules();
}
