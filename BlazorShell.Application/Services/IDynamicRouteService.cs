using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BlazorShell.Application.Models;

namespace BlazorShell.Application.Services;

public interface IDynamicRouteService
{
    event EventHandler<RouteChangedEventArgs> RoutesChanged;

    IReadOnlyList<Assembly> ModuleAssemblies { get; }
    IReadOnlyDictionary<string, RouteInfo> Routes { get; }

    void RegisterModuleAssembly(string moduleName, Assembly assembly);
    void UnregisterModuleAssembly(string moduleName);
    RouteInfo? FindRoute(string path);
    Task RefreshRoutesAsync();
    bool HasRouteConflict(string template);
}
