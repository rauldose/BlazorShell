using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using BlazorShell.Application.Services;
using BlazorShell.Application.Models;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Services;

public class DynamicRouteService : IDynamicRouteService
{
    private readonly ILogger<DynamicRouteService> _logger;
    private readonly Dictionary<string, Assembly> _moduleAssemblies = new();
    private readonly Dictionary<string, RouteInfo> _routes = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public event EventHandler<RouteChangedEventArgs>? RoutesChanged;

    public IReadOnlyList<Assembly> ModuleAssemblies
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _moduleAssemblies.Values.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public IReadOnlyDictionary<string, RouteInfo> Routes
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return new Dictionary<string, RouteInfo>(_routes);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public DynamicRouteService(ILogger<DynamicRouteService> logger)
    {
        _logger = logger;
    }

    public void RegisterModuleAssembly(string moduleName, Assembly assembly)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_moduleAssemblies.ContainsKey(moduleName))
            {
                _logger.LogWarning("Module {ModuleName} assembly already registered, replacing", moduleName);
                UnregisterModuleAssemblyInternal(moduleName);
            }

            _moduleAssemblies[moduleName] = assembly;

            var routes = DiscoverRoutes(assembly, moduleName);
            foreach (var route in routes)
            {
                if (_routes.ContainsKey(route.Template))
                {
                    _logger.LogWarning("Route conflict detected for {Template}. Module {NewModule} conflicts with {ExistingModule}",
                        route.Template, moduleName, _routes[route.Template].ModuleName);
                    _routes[route.Template] = route;
                }
                else
                {
                    _routes[route.Template] = route;
                }
            }

            _logger.LogInformation("Registered {Count} routes from module {ModuleName}", routes.Count, moduleName);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        OnRoutesChanged(moduleName, RouteChangeType.Added);
    }

    public void UnregisterModuleAssembly(string moduleName)
    {
        _lock.EnterWriteLock();
        try
        {
            UnregisterModuleAssemblyInternal(moduleName);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        OnRoutesChanged(moduleName, RouteChangeType.Removed);
    }

    private void UnregisterModuleAssemblyInternal(string moduleName)
    {
        if (!_moduleAssemblies.ContainsKey(moduleName))
            return;

        var routesToRemove = _routes.Where(kvp => kvp.Value.ModuleName == moduleName)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var template in routesToRemove)
        {
            _routes.Remove(template);
        }

        _moduleAssemblies.Remove(moduleName);

        _logger.LogInformation("Unregistered {Count} routes from module {ModuleName}",
            routesToRemove.Count, moduleName);
    }

    public RouteInfo? FindRoute(string path)
    {
        _lock.EnterReadLock();
        try
        {
            path = path.TrimStart('/');

            if (_routes.TryGetValue(path, out var route))
                return route;

            foreach (var kvp in _routes)
            {
                if (MatchesTemplate(path, kvp.Key))
                    return kvp.Value;
            }

            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public async Task RefreshRoutesAsync()
    {
        await Task.Run(() =>
        {
            _lock.EnterWriteLock();
            try
            {
                _routes.Clear();

                foreach (var kvp in _moduleAssemblies)
                {
                    var routes = DiscoverRoutes(kvp.Value, kvp.Key);
                    foreach (var route in routes)
                    {
                        _routes[route.Template] = route;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        });

        OnRoutesChanged("", RouteChangeType.Refreshed);
    }

    public bool HasRouteConflict(string template)
    {
        _lock.EnterReadLock();
        try
        {
            return _routes.ContainsKey(template);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private List<RouteInfo> DiscoverRoutes(Assembly assembly, string moduleName)
    {
        var routes = new List<RouteInfo>();

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsSubclassOf(typeof(Microsoft.AspNetCore.Components.ComponentBase)) || type.IsAbstract)
                continue;

            var routeAttributes = type.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>();
            foreach (var attr in routeAttributes)
            {
                routes.Add(new RouteInfo
                {
                    Template = attr.Template.TrimStart('/'),
                    ComponentType = type,
                    ModuleName = moduleName,
                    Assembly = assembly,
                    Priority = 0
                });
            }
        }

        return routes;
    }

    private bool MatchesTemplate(string path, string template)
    {
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var templateSegments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (pathSegments.Length != templateSegments.Length)
            return false;

        for (int i = 0; i < pathSegments.Length; i++)
        {
            if (templateSegments[i].StartsWith("{"))
                continue;

            if (!string.Equals(pathSegments[i], templateSegments[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private void OnRoutesChanged(string moduleName, RouteChangeType changeType)
    {
        RoutesChanged?.Invoke(this, new RouteChangedEventArgs(moduleName, changeType));
    }
}
